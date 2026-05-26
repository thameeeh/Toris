using UnityEngine;
using UnityEngine.InputSystem;
using OutlandHaven.Inventory;

public class PlayerBowController : MonoBehaviour
{
    private const float MIN_DIRECTION_SQR_MAGNITUDE = 0.0001f;

    [Header("Refs")]
    [SerializeField] private PlayerInputReaderSO _input;
    [SerializeField] private BowSO _bow;
    [SerializeField] private Transform _muzzle;
    [SerializeField] private Transform _muzzleDown;
    [SerializeField] private Transform _muzzleLeft;
    [SerializeField] private Transform _muzzleRight;
    [SerializeField] private Transform _muzzleUp;
    [SerializeField] private PlayerMotor _motor;
    [SerializeField] private Collider2D _ownerCollider;
    [SerializeField] private GameplayPoolManager _poolManager;
    [SerializeField] private PlayerStats _stats;
    [SerializeField] private PlayerFacing _playerFacing;
    [SerializeField] private PlayerEquipmentController _equipment;
    [SerializeField] private PlayerAbilityController _abilityController;
    [Tooltip("Publishes neutral bow outcomes for cross-scene presentation listeners.")]
    [SerializeField] private PlayerBowEventsSO _bowEvents;

    [Header("Spawn Fallback")]
    [Tooltip("Used if muzzle is null. Arrow spawns this far from player along aim.")]
    [SerializeField] private float spawnOffsetFromCenter = 0.35f;

    [Header("Debug")]
    [SerializeField] private bool _enableShootDebugLogs;

    public BowSO BowConfig => _bow;
    public bool IsDrawing => drawing;
    public Vector2 CurrentAimDirection => GetAimDirection();
    public Vector2 CurrentAimWorldPoint => GetPointerWorldPoint();

    public bool CancelCurrentDraw(string reason)
    {
        if (!drawing)
            return false;

        drawing = false;
        _shootReadyRaised = false;
        _overdrawStartedRaised = false;
        _motor?.SetMovementLocked(false);
        LogShoot($"CancelCurrentDraw. reason={reason}");
        DryReleased?.Invoke();
        return true;
    }

    public BowSO.ShotStats BuildFullyDrawnShotStats()
    {
        if (_bow != null)
        {
            return _bow.BuildShotStats(_bow.maxDrawTime, 0f);
        }

        const float fallbackSpeed = 10f;
        const float fallbackDamage = 10f;

        return new BowSO.ShotStats
        {
            power = 1f,
            speed = fallbackSpeed,
            damage = fallbackDamage,
            spreadDeg = 0f
        };
    }

    public event System.Action DrawStarted;
    public event System.Action ShootReady;
    public event System.Action ShotReleased;
    public event System.Action DryReleased;
    // Tutorial/UI consumers listen to semantic shot events instead of polling draw timing.
    public event System.Action UnderdrawReleased;
    public event System.Action OverdrawStarted;
    public event System.Action ShotFired;
    public event System.Action<Vector2> AbilityReleaseRequested;
    public event System.Action<Vector2> BowImpactRequested;

    public void RequestAbilityReleaseTowards(Vector2 worldPoint)
    {
        Vector2 direction = worldPoint - (Vector2)transform.position;
        if (direction.sqrMagnitude < MIN_DIRECTION_SQR_MAGNITUDE)
        {
            direction = GetFallbackFacing();
        }

        AbilityReleaseRequested?.Invoke(direction.normalized);
    }

    private float drawStartTime = -999f;
    private float lastShotTime = -999f;
    private bool drawing;
    private bool _shootReadyRaised;
    private bool _overdrawStartedRaised;
    private Camera _mainCamera;

    private void LogShoot(string message)
    {
        PlayerShootDebug.Log(this, "BowCtrl", message);
    }

    private static string FormatVector(Vector2 value)
    {
        return $"({value.x:F2}, {value.y:F2})";
    }

    private void Awake()
    {
        _mainCamera = Camera.main;

        if (_abilityController == null)
            TryGetComponent(out _abilityController);

        ResolveDirectionalMuzzles();
        SyncShootDebugToggle();
    }

    private void OnEnable()
    {
        SyncShootDebugToggle();

        if (_input != null)
        {
            _input.OnShootStarted += BeginDraw;
            _input.OnShootReleased += ReleaseShot;
        }
    }

    private void OnDisable()
    {
        if (_input != null)
        {
            _input.OnShootStarted -= BeginDraw;
            _input.OnShootReleased -= ReleaseShot;
        }

        _motor?.SetMovementLocked(false);
        drawing = false;
        _shootReadyRaised = false;
        _overdrawStartedRaised = false;
    }

    private void OnValidate()
    {
        if (_abilityController == null)
            TryGetComponent(out _abilityController);

        ResolveDirectionalMuzzles();
        SyncShootDebugToggle();

#if UNITY_EDITOR
        if (_input == null)
        {
            Debug.LogError($"<b><color=red>[PlayerBowController]</color></b> is missing PlayerInputReaderSO on GameObject: <b>{name}<b>", this);
        }
#endif
    }

    private void Update()
    {
        if (!drawing)
            return;

        Vector2 aimDirection = GetAimDirection();
        if (aimDirection.sqrMagnitude > 0.0001f)
        {
            _playerFacing?.SetFacing(aimDirection);
        }

        if (_bow != null)
        {
            float heldTime = Mathf.Max(0f, Time.time - drawStartTime);
            if (!_shootReadyRaised && heldTime >= _bow.nockTime)
            {
                _shootReadyRaised = true;
                LogShoot($"Shoot ready. heldTime={heldTime:F3} nockTime={_bow.nockTime:F3} aim={FormatVector(aimDirection)}");
                ShootReady?.Invoke();
            }

            float overdrawThreshold = Mathf.Max(_bow.nockTime, _bow.overHoldStartsAt);
            if (!_overdrawStartedRaised && heldTime >= overdrawThreshold)
            {
                _overdrawStartedRaised = true;
                LogShoot($"Overdraw started. heldTime={heldTime:F3} overHoldStartsAt={_bow.overHoldStartsAt:F3}");
                OverdrawStarted?.Invoke();
                _bowEvents?.RaiseOverdrawStarted(this);
            }
        }
    }
    private void BeginDraw()
    {
        if (_bow == null)
        {
            LogShoot("BeginDraw ignored. Bow config missing.");
            return;
        }

        if (_motor != null && _motor.isDashing)
        {
            LogShoot("BeginDraw blocked because player is currently dashing.");
            return;
        }

        if (_abilityController != null && _abilityController.IsBowDrawBlocked)
        {
            LogShoot("BeginDraw blocked because an ability is currently using the bow.");
            return;
        }

        float cooldownElapsed = Time.time - lastShotTime;
        if (cooldownElapsed < _bow.cooldownAfterShot)
        {
            LogShoot($"BeginDraw blocked by cooldown. elapsed={cooldownElapsed:F3} cooldown={_bow.cooldownAfterShot:F3}");
            return;
        }

        Vector2 aimDirection = GetAimDirection();
        if (aimDirection.sqrMagnitude > 0.0001f)
        {
            _playerFacing?.SetFacing(aimDirection);
        }

        drawing = true;
        _shootReadyRaised = false;
        _overdrawStartedRaised = false;
        drawStartTime = Time.time;
        _motor?.SetMovementLocked(true);
        LogShoot($"BeginDraw accepted. aim={FormatVector(aimDirection)} nockTime={_bow.nockTime:F3} cooldown={_bow.cooldownAfterShot:F3}");
        DrawStarted?.Invoke();
    }

    private void ReleaseShot()
    {
        if (!drawing)
        {
            _motor?.SetMovementLocked(false);
            LogShoot("ReleaseShot ignored because drawing=false.");
            return;
        }

        drawing = false;
        _shootReadyRaised = false;
        _overdrawStartedRaised = false;
        _motor?.SetMovementLocked(false);

        if (_bow == null)
        {
            LogShoot("ReleaseShot became dry release because bow config is missing.");
            DryReleased?.Invoke();
            return;
        }

        float heldTime = Mathf.Max(0f, Time.time - drawStartTime);
        if (heldTime < _bow.nockTime)
        {
            LogShoot($"Dry release before ready. heldTime={heldTime:F3} nockTime={_bow.nockTime:F3}");
            UnderdrawReleased?.Invoke();
            _bowEvents?.RaiseUnderdrawReleased(this);
            DryReleased?.Invoke();
            return;
        }

        float overHoldExtraSeconds = Mathf.Max(0f, heldTime - _bow.overHoldStartsAt);
        BowSO.ShotStats shot = _bow.BuildShotStats(heldTime, overHoldExtraSeconds);

        Vector2 aimDirection = GetAimDirection();
        if (aimDirection.sqrMagnitude < 0.0001f)
        {
            LogShoot($"Dry release because aim direction is invalid. heldTime={heldTime:F3}");
            DryReleased?.Invoke();
            return;
        }

        _playerFacing?.SetFacing(aimDirection);

        ItemInstance equippedWeapon = _equipment != null
            ? _equipment.GetEquippedItem(EquipmentSlot.Weapon)
            : null;

        float finalShotDamage = shot.damage;

        if (equippedWeapon != null && _stats != null)
        {
            PlayerAttackComputedStats attackStats =
                PlayerCombatCalculator.CalculateAttack(equippedWeapon, _stats, shot.damage);

            if (attackStats.IsValid)
            {
                finalShotDamage = attackStats.FinalAttackDamage;
            }
        }

        BowSO.ShotStats finalShot = new BowSO.ShotStats
        {
            power = shot.power,
            speed = shot.speed,
            damage = finalShotDamage,
            spreadDeg = shot.spreadDeg
        };

        LogShoot(
            $"ReleaseShot firing arrow. heldTime={heldTime:F3} aim={FormatVector(aimDirection)} power={finalShot.power:F2} speed={finalShot.speed:F2} damage={finalShot.damage:F2} spread={finalShot.spreadDeg:F2}");
        FireArrow(finalShot);

        lastShotTime = Time.time;
        LogShoot("ReleaseShot completed. Emitting ShotReleased and ShotFired.");
        ShotReleased?.Invoke();
        ShotFired?.Invoke();
    }

    public void FireArrow(
        BowSO.ShotStats stats,
        bool playReleaseAnimation = false,
        string debugSourceLabel = null,
        float lifetimeOverride = -1f)
    {
        if (_bow == null)
        {
            LogShoot("FireArrow ignored. Bow config missing.");
            return;
        }

        if (_bow.arrowPrefab == null)
        {
            LogShoot("FireArrow ignored. Arrow prefab missing.");
            return;
        }

        BowSO.ShotStats finalStats = ResolveOutgoingDamage(stats);
        SpawnArrowFromAim(finalStats, playReleaseAnimation, true, debugSourceLabel, lifetimeOverride);
    }

    public ArrowProjectile SpawnArrowFromAim(
        BowSO.ShotStats stats,
        bool playReleaseAnimation = false,
        bool applySpread = true,
        string debugSourceLabel = null,
        float lifetimeOverride = -1f)
    {
        if (_bow == null)
        {
            LogShoot("SpawnArrowFromAim ignored. Bow config missing.");
            return null;
        }

        if (_bow.arrowPrefab == null)
        {
            LogShoot("SpawnArrowFromAim ignored. Arrow prefab missing.");
            return null;
        }

        Vector2 baseDir = GetAimDirection();
        if (baseDir.sqrMagnitude < MIN_DIRECTION_SQR_MAGNITUDE)
            baseDir = Vector2.right;

        _playerFacing?.SetFacing(baseDir);
        LogShoot(
            $"SpawnArrowFromAim requested. dir={FormatVector(baseDir)} playReleaseAnimation={playReleaseAnimation} speed={stats.speed:F2} damage={stats.damage:F2} spread={stats.spreadDeg:F2}");

        if (playReleaseAnimation)
        {
            LogShoot("SpawnArrowFromAim requested release animation bridge.");
            AbilityReleaseRequested?.Invoke(baseDir);
        }

        return SpawnArrowInternal(stats, baseDir, applySpread, debugSourceLabel, lifetimeOverride);
    }

    public ArrowProjectile SpawnArrowFromWorld(
        BowSO.ShotStats stats,
        Vector2 dir,
        Vector3 spawnPos,
        bool applySpread = false,
        string spawnSourceLabel = "ability",
        float lifetimeOverride = -1f)
    {
        if (_bow == null)
        {
            LogShoot("SpawnArrowFromWorld ignored. Bow config missing.");
            return null;
        }

        if (_bow.arrowPrefab == null)
        {
            LogShoot("SpawnArrowFromWorld ignored. Arrow prefab missing.");
            return null;
        }

        float lifetimeSeconds = lifetimeOverride > 0f ? lifetimeOverride : _bow.arrowLifetime;
        return SpawnArrowAtPosition(stats, dir, spawnPos, applySpread, spawnSourceLabel, lifetimeSeconds);
    }

    private ArrowProjectile SpawnArrowInternal(
        BowSO.ShotStats stats,
        Vector2 dir,
        bool applySpread,
        string debugSourceLabel = null,
        float lifetimeOverride = -1f)
    {
        Vector3 spawnPos;
        Transform activeMuzzle = GetAimOriginTransform(dir);
        if (activeMuzzle != null)
            spawnPos = activeMuzzle.position;
        else
            spawnPos = transform.position + (Vector3)(dir * spawnOffsetFromCenter);

        string spawnSourceLabel = !string.IsNullOrEmpty(debugSourceLabel)
            ? debugSourceLabel
            : activeMuzzle != null ? activeMuzzle.name : "fallback";
        float lifetimeSeconds = lifetimeOverride > 0f ? lifetimeOverride : _bow.arrowLifetime;
        return SpawnArrowAtPosition(stats, dir, spawnPos, applySpread, spawnSourceLabel, lifetimeSeconds);
    }

    private ArrowProjectile SpawnArrowAtPosition(
        BowSO.ShotStats stats,
        Vector2 dir,
        Vector3 spawnPos,
        bool applySpread,
        string spawnSourceLabel,
        float lifetimeSeconds)
    {
        float spread = applySpread ? Random.Range(-stats.spreadDeg, stats.spreadDeg) : 0f;
        dir = (Quaternion.Euler(0, 0, spread) * (Vector3)dir).normalized;

        var prefabProj = _bow.arrowPrefab;
        if (prefabProj == null)
        {
            LogShoot("SpawnArrow aborted. Prefab missing.");
            return null;
        }

        var manager = _poolManager != null ? _poolManager : GameplayPoolManager.Instance;
        LogShoot(
            $"SpawnArrow. dir={FormatVector(dir)} spreadApplied={spread:F2} muzzle={spawnSourceLabel} spawnPos={spawnPos} speed={stats.speed:F2} damage={stats.damage:F2}");

        Projectile projBase = manager
            ? manager.SpawnProjectile(prefabProj, spawnPos, Quaternion.identity)
            : Instantiate(prefabProj, spawnPos, Quaternion.identity);

        var arrow = projBase as ArrowProjectile;
        if (arrow == null)
        {
            LogShoot("SpawnArrow aborted. Spawned projectile is not ArrowProjectile.");
            return null;
        }

        arrow.SetDebugSource(spawnSourceLabel);
        arrow.Initialize(dir, stats.speed, stats.damage, lifetimeSeconds, _ownerCollider);
        return arrow;
    }

    public Vector2 GetPointerWorldPoint()
    {
        var cam = Camera.main;
        if (!cam)
            return transform.position;

        Vector2 screen;
        if (Pointer.current != null)
            screen = Pointer.current.position.ReadValue();
        else if (Mouse.current != null)
            screen = Mouse.current.position.ReadValue();
        else
            return transform.position;

        Vector3 myPos = transform.position;
        Vector3 depthRef = myPos;
        float depth = cam.orthographic ? 0f : Mathf.Abs(cam.WorldToScreenPoint(depthRef).z);
        Vector3 world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, depth));
        world.z = depthRef.z;
        return world;
    }

    private Vector2 GetAimDirection()
    {
        Vector3 world = GetPointerWorldPoint();
        Vector3 myPos = transform.position;
        Vector2 centerOrigin = (Vector2)myPos;
        Vector2 rawDirection = (Vector2)(world - (Vector3)centerOrigin);
        Vector2 facingDirection = rawDirection.sqrMagnitude > MIN_DIRECTION_SQR_MAGNITUDE
            ? rawDirection.normalized
            : GetFallbackFacing();

        Transform activeMuzzle = GetAimOriginTransform(facingDirection);
        Vector2 origin = activeMuzzle != null ? (Vector2)activeMuzzle.position : centerOrigin;
        Vector2 aimedDirection = (Vector2)(world - (Vector3)origin);

        return aimedDirection.sqrMagnitude > MIN_DIRECTION_SQR_MAGNITUDE
            ? aimedDirection.normalized
            : facingDirection;
    }

    public void FireMultiShotVolley(BowSO.ShotStats stats, int arrowCount, float totalSpreadDegrees, bool playReleaseAnimation = false)
    {
        BowSO.ShotStats finalStats = ResolveOutgoingDamage(stats);

        Vector2 baseDir = GetAimDirection();
        if (baseDir.sqrMagnitude < 0.0001f)
            baseDir = Vector2.right;

        _playerFacing?.SetFacing(baseDir);

        LogShoot(
            $"FireMultiShotVolley requested. count={arrowCount} totalSpread={totalSpreadDegrees:F2} dir={FormatVector(baseDir)} playReleaseAnimation={playReleaseAnimation}");

        if (playReleaseAnimation)
        {
            LogShoot("FireMultiShotVolley requested release animation bridge.");
            AbilityReleaseRequested?.Invoke(baseDir);
        }

        int count = Mathf.Max(1, arrowCount);
        for (int i = 0; i < count; i++)
        {
            float t = count == 1 ? 0.5f : (float)i / (count - 1);
            float angle = Mathf.Lerp(-totalSpreadDegrees * 0.5f, totalSpreadDegrees * 0.5f, t);
            Vector2 dir = (Quaternion.Euler(0, 0, angle) * (Vector3)baseDir).normalized;
            SpawnArrowInternal(finalStats, dir, true);
        }
    }

    public BowSO.ShotStats ResolveOutgoingDamage(BowSO.ShotStats stats)
    {
        stats.damage = ResolveOutgoingDamage(stats.damage);
        return stats;
    }

    public float ResolveOutgoingDamage(float damage)
    {
        const float minDamageMultiplier = 0f;

        float finalDamage = Mathf.Max(0f, damage);
        float outgoingDamageMultiplier = 1f;

        if (_stats != null)
        {
            outgoingDamageMultiplier = Mathf.Max(
                minDamageMultiplier,
                _stats.ResolvedEffects.outgoingDamageMultiplier);
        }

        finalDamage *= outgoingDamageMultiplier;
        return finalDamage;
    }

    private void SyncShootDebugToggle()
    {
        PlayerShootDebug.SetEnabled(_enableShootDebugLogs);
    }

    private void ResolveDirectionalMuzzles()
    {
        if (_muzzleDown == null)
            _muzzleDown = FindChildTransform("Muzzle_D");

        if (_muzzleLeft == null)
            _muzzleLeft = FindChildTransform("Muzzle_L");

        if (_muzzleRight == null)
            _muzzleRight = FindChildTransform("Muzzle_R");

        if (_muzzleUp == null)
            _muzzleUp = FindChildTransform("Muzzle_U");
    }

    private Transform FindChildTransform(string childName)
    {
        Transform[] childTransforms = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < childTransforms.Length; i++)
        {
            Transform child = childTransforms[i];
            if (child != null && child.name == childName)
                return child;
        }

        return null;
    }

    private Transform GetAimOriginTransform(Vector2 direction)
    {
        Transform directionalMuzzle = GetDirectionalMuzzle(direction);
        if (directionalMuzzle != null)
            return directionalMuzzle;

        if (_muzzle != null)
            return _muzzle;

        return null;
    }

    private Transform GetDirectionalMuzzle(Vector2 direction)
    {
        if (direction.sqrMagnitude <= MIN_DIRECTION_SQR_MAGNITUDE)
            direction = GetFallbackFacing();

        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            return direction.x >= 0f ? _muzzleRight : _muzzleLeft;

        return direction.y >= 0f ? _muzzleUp : _muzzleDown;
    }

    private Vector2 GetFallbackFacing()
    {
        if (_playerFacing != null && _playerFacing.CurrentFacing.sqrMagnitude > MIN_DIRECTION_SQR_MAGNITUDE)
            return _playerFacing.CurrentFacing;

        return Vector2.down;
    }

    public void PlayDefaultArrowHitEffect(Vector2 position)
    {
        BowImpactRequested?.Invoke(position);
    }
}
