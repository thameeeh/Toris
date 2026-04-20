using UnityEngine;
using UnityEngine.InputSystem;
using OutlandHaven.Inventory;

public class PlayerBowController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerInputReaderSO _input;
    [SerializeField] private BowSO _bow;
    [SerializeField] private Transform _muzzle;
    [SerializeField] private PlayerMotor _motor;
    [SerializeField] private Collider2D _ownerCollider;
    [SerializeField] private GameplayPoolManager _poolManager;
    [SerializeField] private PlayerStats _stats;
    [SerializeField] private PlayerFacing _playerFacing;
    [SerializeField] private PlayerEquipmentController _equipment;

    [Header("Spawn Fallback")]
    [Tooltip("Used if muzzle is null. Arrow spawns this far from player along aim.")]
    [SerializeField] private float spawnOffsetFromCenter = 0.35f;

    public BowSO BowConfig => _bow;
    public bool IsDrawing => drawing;
    public Vector2 CurrentAimDirection => GetAimDirection();
    public Vector2 CurrentAimWorldPoint => GetPointerWorldPoint();

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
    public event System.Action ShotReleased;
    public event System.Action DryReleased;
    public event System.Action ShotFired;
    public event System.Action<Vector2, bool> AbilityReleaseRequested;

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
    private bool _currentDrawNocked;

    private void OnEnable()
    {
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
        _currentDrawNocked = false;
    }

    private void OnValidate()
    {
        if (_input == null)
        {
            Debug.LogError($"<b><color=red>[PlayerBowController]</color></b> is missing PlayerInputReaderSO on GameObject: <b>{name}<b>", this);
        }
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
    }
    private void BeginDraw()
    {
        if (_bow == null) return;
        if (Time.time - lastShotTime < _bow.cooldownAfterShot) return;

        Vector2 aimDirection = GetAimDirection();
        if (aimDirection.sqrMagnitude > 0.0001f)
        {
            _playerFacing?.SetFacing(aimDirection);
        }

        drawing = true;
        _currentDrawNocked = false;
        drawStartTime = Time.time;
        _motor?.SetMovementLocked(true);
        DrawStarted?.Invoke();
    }

    public void NotifyNockReached()
    {
        if (drawing)
        {
            _currentDrawNocked = true;
        }
    }

    private void ReleaseShot()
    {
        if (!drawing)
        {
            _motor?.SetMovementLocked(false);
            return;
        }

        drawing = false;
        _motor?.SetMovementLocked(false);

        if (_bow == null)
        {
            _currentDrawNocked = false;
            DryReleased?.Invoke();
            return;
        }

        if (!_currentDrawNocked)
        {
            _currentDrawNocked = false;
            DryReleased?.Invoke();
            return;
        }

        float heldTime = Mathf.Max(0f, Time.time - drawStartTime);
        _currentDrawNocked = false;
        BowSO.ShotStats shot = _bow.BuildShotStats(heldTime, 0f);

        Vector2 aimDirection = GetAimDirection();
        if (aimDirection.sqrMagnitude < 0.0001f)
        {
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

        FireArrow(finalShot);

        lastShotTime = Time.time;
        ShotReleased?.Invoke();
        ShotFired?.Invoke();
    }

    public void FireArrow(BowSO.ShotStats stats, bool playReleaseAnimation = false, bool useShortReleaseAnimation = true)
    {
        if (_bow == null) return;
        if (_bow.arrowPrefab == null)
            return;

        BowSO.ShotStats finalStats = ResolveOutgoingDamage(stats);
        SpawnArrowFromAim(finalStats, playReleaseAnimation, true);
    }

    public ArrowProjectile SpawnArrowFromAim(BowSO.ShotStats stats, bool playReleaseAnimation = false, bool applySpread = true)
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
<<<<<<< HEAD
        LogShoot(
            $"SpawnArrowFromAim requested. dir={FormatVector(baseDir)} playReleaseAnimation={playReleaseAnimation} speed={stats.speed:F2} damage={stats.damage:F2} spread={stats.spreadDeg:F2}");

        if (playReleaseAnimation)
        {
            LogShoot("SpawnArrowFromAim requested release animation bridge.");
            AbilityReleaseRequested?.Invoke(baseDir);
=======

        if (playReleaseAnimation)
        {
            AbilityReleaseRequested?.Invoke(baseDir, useShortReleaseAnimation);
>>>>>>> UI_Update
        }

        return SpawnArrowInternal(stats, baseDir, applySpread);
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

    private ArrowProjectile SpawnArrowInternal(BowSO.ShotStats stats, Vector2 dir, bool applySpread)
    {
        Vector3 spawnPos;
        if (_muzzle != null)
            spawnPos = _muzzle.position;
        else
            spawnPos = transform.position + (Vector3)(dir * spawnOffsetFromCenter);

        string spawnSourceLabel = activeMuzzle != null ? activeMuzzle.name : "fallback";
        return SpawnArrowAtPosition(stats, dir, spawnPos, applySpread, spawnSourceLabel, _bow.arrowLifetime);
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
<<<<<<< HEAD
        {
            LogShoot("SpawnArrow aborted. Prefab missing.");
            return null;
        }

        var manager = _poolManager != null ? _poolManager : GameplayPoolManager.Instance;
        LogShoot(
            $"SpawnArrow. dir={FormatVector(dir)} spreadApplied={spread:F2} muzzle={spawnSourceLabel} spawnPos={spawnPos} speed={stats.speed:F2} damage={stats.damage:F2}");
=======
            return;

        var manager = _poolManager != null ? _poolManager : GameplayPoolManager.Instance;
>>>>>>> UI_Update

        Projectile projBase = manager
            ? manager.SpawnProjectile(prefabProj, spawnPos, Quaternion.identity)
            : Instantiate(prefabProj, spawnPos, Quaternion.identity);

        var arrow = projBase as ArrowProjectile;
        if (arrow == null)
<<<<<<< HEAD
        {
            LogShoot("SpawnArrow aborted. Spawned projectile is not ArrowProjectile.");
            return null;
        }
=======
            return;
>>>>>>> UI_Update

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

        Vector3 depthRef = _muzzle ? _muzzle.position : transform.position;
        float depth = cam.orthographic ? 0f : Mathf.Abs(cam.WorldToScreenPoint(depthRef).z);
        Vector3 world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, depth));
        world.z = depthRef.z;
        return world;
    }

    private Vector2 GetAimDirection()
    {
        Vector3 world = GetPointerWorldPoint();

        Vector2 origin = _muzzle ? (Vector2)_muzzle.position : (Vector2)transform.position;
        Vector2 v = (Vector2)(world - (Vector3)origin);
        return v.sqrMagnitude > 0.0001f ? v.normalized : Vector2.right;
    }

    public void FireMultiShotVolley(BowSO.ShotStats stats, int arrowCount, float totalSpreadDegrees, bool playReleaseAnimation = false, bool useShortReleaseAnimation = true)
    {
        BowSO.ShotStats finalStats = ResolveOutgoingDamage(stats);

        Vector2 baseDir = GetAimDirection();
        if (baseDir.sqrMagnitude < 0.0001f)
            baseDir = Vector2.right;

        _playerFacing?.SetFacing(baseDir);

        if (playReleaseAnimation)
        {
            AbilityReleaseRequested?.Invoke(baseDir, useShortReleaseAnimation);
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
<<<<<<< HEAD

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
        if (EffectManagerBehavior.Instance == null)
            return;

        EffectRequest request = new EffectRequest
        {
            EffectId = "hit_arrow_square",
            Position = position,
            Rotation = Quaternion.identity,
            Parent = null,
            Variant = default,
            Magnitude = 1f
        };

        EffectManagerBehavior.Instance.Play(request);
    }
=======
>>>>>>> UI_Update
}
