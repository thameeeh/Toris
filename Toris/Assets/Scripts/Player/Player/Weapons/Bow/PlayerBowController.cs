using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using OutlandHaven.Inventory;

public class PlayerBowController : MonoBehaviour
{
    private const float MIN_DIRECTION_SQR_MAGNITUDE = 0.0001f;
    private const int ArrowRainMaxOverlapResults = 16;
    private const int ChainShotMaxOverlapResults = 16;
    private const string ArrowRainHitEffectId = "hit_arrow_square";
    private const float ArrowRainVisualLifetimePadding = 0.05f;
    private const float ChainShotSpawnOffset = 0.05f;
    private const string EnemyHurtBoxLayerName = "EnemyHurtBox";

    [System.Serializable]
    public struct ChainShotSettings
    {
        public float baseDamage;
        public float[] damageMultipliers;
        public float chainSearchRadius;
        public float chainProjectileSpeed;
        public float chainProjectileLifetime;
        public float chainJumpDelay;
        public bool playImpactEffect;
    }

    [System.Serializable]
    public struct ArrowRainZoneSettings
    {
        public Vector2 castOrigin;
        public float maxTargetRange;
        public float duration;
        public float firstBurstDelay;
        public float burstInterval;
        public int strikesPerBurst;
        public float zoneRadius;
        public float impactRadius;
        public bool guaranteeCenterStrike;
        public float damagePerStrike;
        public GameObject arrowRainVisualPrefab;
        public bool spawnVisualArrows;
        public float visualArrowHeight;
        public float visualArrowSpeed;
        public bool playImpactEffect;
    }

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
    public event System.Action ShotFired;
    public event System.Action<Vector2> AbilityReleaseRequested;

    public void RequestAbilityReleaseTowards(Vector2 worldPoint)
    {
        Vector2 direction = worldPoint - (Vector2)transform.position;
        if (direction.sqrMagnitude < MIN_DIRECTION_SQR_MAGNITUDE)
        {
            direction = GetFallbackFacing();
        }

        AbilityReleaseRequested?.Invoke(direction.normalized);
    }

    public void StartArrowRain(Vector2 center, ArrowRainZoneSettings settings)
    {
        if (!isActiveAndEnabled)
            return;

        Vector2 clampedCenter = ClampArrowRainPointToRange(center, settings);
        _arrowRainCastVersion++;

        if (_arrowRainRoutine != null)
        {
            StopCoroutine(_arrowRainRoutine);
            _arrowRainRoutine = null;
        }

        LogShoot(
            $"ArrowRain cast accepted. castOrigin={FormatVector(settings.castOrigin)} requestedCenter={FormatVector(center)} clampedCenter={FormatVector(clampedCenter)} maxTargetRange={settings.maxTargetRange:F2} zoneRadius={settings.zoneRadius:F2}");
        _arrowRainRoutine = StartCoroutine(RunArrowRainZone(clampedCenter, settings, _arrowRainCastVersion));
    }

    private float drawStartTime = -999f;
    private float lastShotTime = -999f;
    private bool drawing;
    private bool _shootReadyRaised;
    private Coroutine _arrowRainRoutine;
    private int _arrowRainCastVersion;
    private readonly Collider2D[] _arrowRainOverlapResults = new Collider2D[ArrowRainMaxOverlapResults];
    private readonly HashSet<IDamageable> _arrowRainDamagedTargets = new HashSet<IDamageable>();
    private readonly Collider2D[] _chainShotOverlapResults = new Collider2D[ChainShotMaxOverlapResults];

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
        _arrowRainCastVersion++;

        if (_arrowRainRoutine != null)
        {
            StopCoroutine(_arrowRainRoutine);
            _arrowRainRoutine = null;
        }
    }

    private void OnValidate()
    {
        if (_abilityController == null)
            TryGetComponent(out _abilityController);

        ResolveDirectionalMuzzles();
        SyncShootDebugToggle();

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

        if (_bow != null && !_shootReadyRaised)
        {
            float heldTime = Mathf.Max(0f, Time.time - drawStartTime);
            if (heldTime >= _bow.nockTime)
            {
                _shootReadyRaised = true;
                LogShoot($"Shoot ready. heldTime={heldTime:F3} nockTime={_bow.nockTime:F3} aim={FormatVector(aimDirection)}");
                ShootReady?.Invoke();
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
            DryReleased?.Invoke();
            return;
        }

        BowSO.ShotStats shot = _bow.BuildShotStats(heldTime, 0f);

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

    public void FireArrow(BowSO.ShotStats stats, bool playReleaseAnimation = false)
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

        BowSO.ShotStats finalStats = ApplyResolvedDamageModifiers(stats);

        Vector2 baseDir = GetAimDirection();
        if (baseDir.sqrMagnitude < 0.0001f)
            baseDir = Vector2.right;

        _playerFacing?.SetFacing(baseDir);

        LogShoot(
            $"FireArrow requested. dir={FormatVector(baseDir)} playReleaseAnimation={playReleaseAnimation} speed={finalStats.speed:F2} damage={finalStats.damage:F2} spread={finalStats.spreadDeg:F2}");

        if (playReleaseAnimation)
        {
            LogShoot("FireArrow requested release animation bridge.");
            AbilityReleaseRequested?.Invoke(baseDir);
        }

        SpawnArrow(finalStats, baseDir);
    }

    public void FireChainShot(BowSO.ShotStats initialShotStats, ChainShotSettings settings, bool playReleaseAnimation = false)
    {
        if (_bow == null)
        {
            LogShoot("FireChainShot ignored. Bow config missing.");
            return;
        }

        if (_bow.arrowPrefab == null)
        {
            LogShoot("FireChainShot ignored. Arrow prefab missing.");
            return;
        }

        if (settings.damageMultipliers == null || settings.damageMultipliers.Length == 0)
        {
            LogShoot("FireChainShot fell back to normal arrow because no damage multipliers were configured.");
            FireArrow(initialShotStats, playReleaseAnimation);
            return;
        }

        BowSO.ShotStats finalInitialShot = ApplyResolvedDamageModifiers(initialShotStats);

        Vector2 baseDir = GetAimDirection();
        if (baseDir.sqrMagnitude < MIN_DIRECTION_SQR_MAGNITUDE)
            baseDir = Vector2.right;

        _playerFacing?.SetFacing(baseDir);

        LogShoot(
            $"FireChainShot requested. dir={FormatVector(baseDir)} firstDamage={finalInitialShot.damage:F2} firstSpeed={finalInitialShot.speed:F2} chainTargets={settings.damageMultipliers.Length} searchRadius={settings.chainSearchRadius:F2}");

        if (playReleaseAnimation)
        {
            LogShoot("FireChainShot requested release animation bridge.");
            AbilityReleaseRequested?.Invoke(baseDir);
        }

        HashSet<int> visitedTargets = new HashSet<int>();
        ArrowProjectile projectile = SpawnArrowInternal(finalInitialShot, baseDir);
        if (projectile == null)
            return;

        ConfigureChainShotProjectile(projectile, settings, 1, visitedTargets);
    }

    private void SpawnArrow(BowSO.ShotStats stats, Vector2 dir)
    {
        SpawnArrowInternal(stats, dir);
    }

    private ArrowProjectile SpawnArrowInternal(BowSO.ShotStats stats, Vector2 dir)
    {
        Vector3 spawnPos;
        Transform activeMuzzle = GetAimOriginTransform(dir);
        if (activeMuzzle != null)
            spawnPos = activeMuzzle.position;
        else
            spawnPos = transform.position + (Vector3)(dir * spawnOffsetFromCenter);

        string spawnSourceLabel = activeMuzzle != null ? activeMuzzle.name : "fallback";
        return SpawnArrowAtPosition(stats, dir, spawnPos, true, spawnSourceLabel);
    }

    private ArrowProjectile SpawnArrowAtPosition(BowSO.ShotStats stats, Vector2 dir, Vector3 spawnPos, bool applySpread, string spawnSourceLabel)
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

        arrow.Initialize(dir, stats.speed, stats.damage, _bow.arrowLifetime, _ownerCollider);
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

        Vector3 depthRef = transform.position;
        float depth = cam.orthographic ? 0f : Mathf.Abs(cam.WorldToScreenPoint(depthRef).z);
        Vector3 world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, depth));
        world.z = depthRef.z;
        return world;
    }

    private Vector2 GetAimDirection()
    {
        Vector3 world = GetPointerWorldPoint();

        Vector2 centerOrigin = transform.position;
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
        BowSO.ShotStats finalStats = ApplyResolvedDamageModifiers(stats);

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
            SpawnArrow(finalStats, dir);
        }
    }

    private BowSO.ShotStats ApplyResolvedDamageModifiers(BowSO.ShotStats stats)
    {
        stats.damage = ApplyResolvedDamageModifiers(stats.damage);
        return stats;
    }

    private float ApplyResolvedDamageModifiers(float damage)
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

    private IEnumerator RunArrowRainZone(Vector2 center, ArrowRainZoneSettings settings, int castVersion)
    {
        float safeDuration = Mathf.Max(0.01f, settings.duration);
        float safeFirstBurstDelay = Mathf.Clamp(settings.firstBurstDelay, 0f, safeDuration);
        float safeBurstInterval = Mathf.Max(0.01f, settings.burstInterval);
        float elapsed = safeFirstBurstDelay;

        LogShoot(
            $"ArrowRain started. center={FormatVector(center)} zoneRadius={settings.zoneRadius:F2} duration={safeDuration:F2} burstInterval={safeBurstInterval:F2} strikesPerBurst={settings.strikesPerBurst}");

        if (safeFirstBurstDelay > 0f)
        {
            yield return new WaitForSeconds(safeFirstBurstDelay);
        }

        bool firstBurst = true;
        while (elapsed <= safeDuration + 0.0001f && castVersion == _arrowRainCastVersion)
        {
            ResolveArrowRainBurst(center, settings, firstBurst, castVersion);
            firstBurst = false;

            if (elapsed + safeBurstInterval > safeDuration)
                break;

            yield return new WaitForSeconds(safeBurstInterval);
            elapsed += safeBurstInterval;
        }

        if (castVersion == _arrowRainCastVersion)
            _arrowRainRoutine = null;
    }

    private void ResolveArrowRainBurst(Vector2 center, ArrowRainZoneSettings settings, bool isFirstBurst, int castVersion)
    {
        int strikeCount = Mathf.Max(1, settings.strikesPerBurst);
        float finalDamage = ApplyResolvedDamageModifiers(settings.damagePerStrike);
        float zoneRadius = Mathf.Max(0f, settings.zoneRadius);

        LogShoot(
            $"ArrowRain burst. center={FormatVector(center)} firstBurst={isFirstBurst} strikeCount={strikeCount} damage={finalDamage:F2}");

        int remainingStrikes = strikeCount;
        if (isFirstBurst && settings.guaranteeCenterStrike)
        {
            QueueArrowRainStrike(center, settings, finalDamage, castVersion);
            remainingStrikes--;
        }

        if (remainingStrikes <= 0)
            return;

        if (isFirstBurst)
        {
            float ringRadius = zoneRadius * 0.65f;
            float angleStep = 360f / remainingStrikes;
            for (int i = 0; i < remainingStrikes; i++)
            {
                float angle = i * angleStep;
                Vector2 offset = Quaternion.Euler(0f, 0f, angle) * Vector2.right * ringRadius;
                QueueArrowRainStrike(center + offset, settings, finalDamage, castVersion);
            }
            return;
        }

        for (int i = 0; i < remainingStrikes; i++)
        {
            Vector2 strikePoint = center + (Random.insideUnitCircle * zoneRadius);
            QueueArrowRainStrike(strikePoint, settings, finalDamage, castVersion);
        }
    }

    private void QueueArrowRainStrike(Vector2 strikePoint, ArrowRainZoneSettings settings, float damage, int castVersion)
    {
        Vector2 clampedStrikePoint = ClampArrowRainPointToRange(strikePoint, settings);
        float strikeDistance = Vector2.Distance(settings.castOrigin, clampedStrikePoint);
        LogShoot(
            $"ArrowRain strike queued. requested={FormatVector(strikePoint)} clamped={FormatVector(clampedStrikePoint)} distanceFromCastOrigin={strikeDistance:F2} maxTargetRange={settings.maxTargetRange:F2}");
        float impactDelay = SpawnArrowRainVisual(clampedStrikePoint, settings);
        StartCoroutine(ResolveArrowRainStrikeAfterDelay(clampedStrikePoint, settings, damage, impactDelay, castVersion));
    }

    private IEnumerator ResolveArrowRainStrikeAfterDelay(
        Vector2 strikePoint,
        ArrowRainZoneSettings settings,
        float damage,
        float impactDelay,
        int castVersion)
    {
        if (impactDelay > 0f)
            yield return new WaitForSeconds(impactDelay);

        if (castVersion != _arrowRainCastVersion)
            yield break;

        PlayArrowRainImpactEffect(strikePoint, settings);

        float impactRadius = settings.impactRadius;
        float safeImpactRadius = Mathf.Max(0.05f, impactRadius);
        int hitCount = Physics2D.OverlapCircleNonAlloc(strikePoint, safeImpactRadius, _arrowRainOverlapResults);
        LogShoot(
            $"ArrowRain strike resolved. point={FormatVector(strikePoint)} distanceFromCastOrigin={Vector2.Distance(settings.castOrigin, strikePoint):F2} impactRadius={safeImpactRadius:F2} hitCount={hitCount}");
        _arrowRainDamagedTargets.Clear();

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D overlapCollider = _arrowRainOverlapResults[i];
            if (overlapCollider == null || overlapCollider == _ownerCollider)
                continue;

            if (!IsEnemyHurtBoxCollider(overlapCollider))
            {
                LogShoot($"ArrowRain skipped non-hurtbox collider. collider={overlapCollider.name} layer={LayerMask.LayerToName(overlapCollider.gameObject.layer)}");
                continue;
            }

            IDamageable damageable = overlapCollider.GetComponentInParent<IDamageable>();
            if (damageable == null || !_arrowRainDamagedTargets.Add(damageable))
                continue;

            Vector2 closestPoint = overlapCollider.ClosestPoint(strikePoint);
            Component damageableComponent = damageable as Component;
            string targetName = damageableComponent != null ? damageableComponent.name : damageable.GetType().Name;
            Vector2 targetPosition = damageableComponent != null
                ? (Vector2)damageableComponent.transform.position
                : closestPoint;
            LogShoot(
                $"ArrowRain damage hit. target={targetName} collider={overlapCollider.name} strikePoint={FormatVector(strikePoint)} closestPoint={FormatVector(closestPoint)} closestDistanceFromStrike={Vector2.Distance(strikePoint, closestPoint):F2} targetPosition={FormatVector(targetPosition)} targetDistanceFromCastOrigin={Vector2.Distance(settings.castOrigin, targetPosition):F2}");
            damageable.Damage(damage);
        }
    }

    private float SpawnArrowRainVisual(Vector2 strikePoint, ArrowRainZoneSettings settings)
    {
        if (!settings.spawnVisualArrows || settings.arrowRainVisualPrefab == null)
            return 0f;

        float safeHeight = Mathf.Max(0f, settings.visualArrowHeight);
        float safeSpeed = Mathf.Max(0.1f, settings.visualArrowSpeed);
        float flightTime = safeHeight / safeSpeed;
        Vector3 spawnPoint = strikePoint + (Vector2.up * safeHeight);
        GameObject visualObject = Instantiate(settings.arrowRainVisualPrefab, spawnPoint, Quaternion.identity);
        ArrowRainVisual visual = visualObject.GetComponent<ArrowRainVisual>();

        if (visual == null)
        {
            Destroy(visualObject);
            return 0f;
        }

        visual.Initialize(
            spawnPoint,
            strikePoint,
            flightTime + ArrowRainVisualLifetimePadding);

        return flightTime;
    }

    private void PlayArrowRainImpactEffect(Vector2 strikePoint, ArrowRainZoneSettings settings)
    {
        if (!settings.playImpactEffect || EffectManagerBehavior.Instance == null)
            return;

        PlayHitEffect(strikePoint);
    }

    private Vector2 ClampArrowRainPointToRange(Vector2 point, ArrowRainZoneSettings settings)
    {
        if (settings.maxTargetRange <= 0f)
            return point;

        Vector2 offset = point - settings.castOrigin;
        float maxRangeSqr = settings.maxTargetRange * settings.maxTargetRange;
        if (offset.sqrMagnitude <= maxRangeSqr)
            return point;

        if (offset.sqrMagnitude <= MIN_DIRECTION_SQR_MAGNITUDE)
            return settings.castOrigin;

        return settings.castOrigin + (offset.normalized * settings.maxTargetRange);
    }

    private void ConfigureChainShotProjectile(
        ArrowProjectile projectile,
        ChainShotSettings settings,
        int nextDamageIndex,
        HashSet<int> visitedTargets)
    {
        if (projectile == null)
            return;

        int enemyHurtBoxLayer = LayerMask.NameToLayer(EnemyHurtBoxLayerName);
        if (enemyHurtBoxLayer >= 0)
            projectile.SetDamageLayerMask(1 << enemyHurtBoxLayer);

        projectile.SetPlayHitEffect(settings.playImpactEffect);

        projectile.SetDamageTargetPredicate(targetCollider =>
        {
            if (!IsEnemyHurtBoxCollider(targetCollider))
                return false;

            IDamageable damageableTarget = targetCollider.GetComponentInParent<IDamageable>();
            if (damageableTarget == null)
                return false;

            Component damageableComponent = damageableTarget as Component;
            if (damageableComponent != null && visitedTargets.Contains(damageableComponent.GetInstanceID()))
                return false;

            return true;
        });

        System.Action<ArrowProjectile, Collider2D, IDamageable, Vector2> handleHit = null;
        handleHit = (hitProjectile, hitCollider, damageable, hitPoint) =>
        {
            hitProjectile.DamageApplied -= handleHit;

            if (!IsEnemyHurtBoxCollider(hitCollider))
                return;

            Component hitComponent = damageable as Component;
            if (hitComponent != null)
                visitedTargets.Add(hitComponent.GetInstanceID());

            LogShoot(
                $"ChainShot hit confirmed. target={(hitComponent != null ? hitComponent.name : damageable.GetType().Name)} hitPoint={FormatVector(hitPoint)} nextDamageIndex={nextDamageIndex}");

            StartCoroutine(ContinueChainShotAfterHit(hitPoint, hitCollider, settings, nextDamageIndex, visitedTargets));
        };

        projectile.DamageApplied += handleHit;
    }

    private IEnumerator ContinueChainShotAfterHit(
        Vector2 currentPoint,
        Collider2D previousHitCollider,
        ChainShotSettings settings,
        int nextDamageIndex,
        HashSet<int> visitedTargets)
    {
        if (settings.damageMultipliers == null || nextDamageIndex >= settings.damageMultipliers.Length)
            yield break;

        float safeJumpDelay = Mathf.Max(0f, settings.chainJumpDelay);
        if (safeJumpDelay > 0f)
            yield return new WaitForSeconds(safeJumpDelay);

        SpawnNextChainShotProjectile(currentPoint, previousHitCollider, settings, nextDamageIndex, visitedTargets);
    }

    private Collider2D FindNearestChainShotTarget(Vector2 origin, float searchRadius, HashSet<int> visitedTargets)
    {
        int enemyHurtBoxLayer = LayerMask.NameToLayer(EnemyHurtBoxLayerName);
        int layerMask = enemyHurtBoxLayer >= 0 ? 1 << enemyHurtBoxLayer : Physics2D.DefaultRaycastLayers;
        int hitCount = Physics2D.OverlapCircleNonAlloc(origin, searchRadius, _chainShotOverlapResults, layerMask);

        Collider2D nearestCollider = null;
        float nearestDistanceSqr = float.PositiveInfinity;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D overlapCollider = _chainShotOverlapResults[i];
            if (overlapCollider == null || !IsEnemyHurtBoxCollider(overlapCollider))
                continue;

            IDamageable damageable = overlapCollider.GetComponentInParent<IDamageable>();
            if (damageable == null)
                continue;

            Component damageableComponent = damageable as Component;
            if (damageableComponent != null && visitedTargets.Contains(damageableComponent.GetInstanceID()))
                continue;

            Vector2 candidatePoint = overlapCollider.ClosestPoint(origin);
            float distanceSqr = (candidatePoint - origin).sqrMagnitude;
            if (distanceSqr < nearestDistanceSqr)
            {
                nearestDistanceSqr = distanceSqr;
                nearestCollider = overlapCollider;
            }
        }

        return nearestCollider;
    }

    private void SpawnNextChainShotProjectile(
        Vector2 startPoint,
        Collider2D previousHitCollider,
        ChainShotSettings settings,
        int damageIndex,
        HashSet<int> visitedTargets)
    {
        float safeSearchRadius = Mathf.Max(0.05f, settings.chainSearchRadius);
        Collider2D nextTargetCollider = FindNearestChainShotTarget(startPoint, safeSearchRadius, visitedTargets);
        if (nextTargetCollider == null)
        {
            LogShoot($"ChainShot stopped. No valid target found from point={FormatVector(startPoint)} within radius={safeSearchRadius:F2}");
            return;
        }

        if (settings.damageMultipliers == null || damageIndex >= settings.damageMultipliers.Length)
            return;

        Vector2 targetPoint = nextTargetCollider.ClosestPoint(startPoint);
        Vector2 direction = targetPoint - startPoint;
        if (direction.sqrMagnitude <= MIN_DIRECTION_SQR_MAGNITUDE)
            return;

        float safeChainSpeed = Mathf.Max(0.1f, settings.chainProjectileSpeed);
        float safeChainLifetime = Mathf.Max(0.05f, settings.chainProjectileLifetime);
        float damageMultiplier = settings.damageMultipliers[damageIndex];
        BowSO.ShotStats chainShotStats = new BowSO.ShotStats
        {
            power = 1f,
            speed = safeChainSpeed,
            damage = ApplyResolvedDamageModifiers(settings.baseDamage * damageMultiplier),
            spreadDeg = 0f
        };

        float escapeDistance = ChainShotSpawnOffset;
        if (previousHitCollider != null)
            escapeDistance += previousHitCollider.bounds.extents.magnitude;

        Vector3 spawnPoint = startPoint + (Vector2)(direction.normalized * escapeDistance);
        ArrowProjectile nextProjectile = SpawnArrowAtPosition(chainShotStats, direction.normalized, spawnPoint, false, "chain");
        if (nextProjectile == null)
            return;

        LogShoot(
            $"ChainShot bounce spawned. index={damageIndex} from={FormatVector(startPoint)} to={FormatVector(targetPoint)} damage={chainShotStats.damage:F2} speed={chainShotStats.speed:F2}");

        nextProjectile.Initialize(direction.normalized, safeChainSpeed, chainShotStats.damage, safeChainLifetime, _ownerCollider);
        ConfigureChainShotProjectile(nextProjectile, settings, damageIndex + 1, visitedTargets);
    }

    private void PlayHitEffect(Vector2 position)
    {
        if (EffectManagerBehavior.Instance == null)
            return;

        EffectRequest request = new EffectRequest
        {
            EffectId = ArrowRainHitEffectId,
            Position = position,
            Rotation = Quaternion.identity,
            Parent = null,
            Variant = default,
            Magnitude = 1f
        };

        EffectManagerBehavior.Instance.Play(request);
    }

    private static bool IsEnemyHurtBoxCollider(Collider2D overlapCollider)
    {
        int enemyHurtBoxLayer = LayerMask.NameToLayer(EnemyHurtBoxLayerName);
        if (enemyHurtBoxLayer < 0)
            return true;

        return overlapCollider.gameObject.layer == enemyHurtBoxLayer;
    }
}
