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

    private float drawStartTime = -999f;
    private float lastShotTime = -999f;
    private bool drawing;

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
        drawStartTime = Time.time;
        _motor?.SetMovementLocked(true);
        DrawStarted?.Invoke();
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
            DryReleased?.Invoke();
            return;
        }

        float heldTime = Mathf.Max(0f, Time.time - drawStartTime);
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

    public void FireArrow(BowSO.ShotStats stats)
    {
        if (_bow == null) return;
        if (_bow.arrowPrefab == null)
            return;

        BowSO.ShotStats finalStats = ApplyResolvedDamageModifiers(stats);

        Vector2 baseDir = GetAimDirection();
        if (baseDir.sqrMagnitude < 0.0001f)
            baseDir = Vector2.right;

        _playerFacing?.SetFacing(baseDir);
        SpawnArrow(finalStats, baseDir);
    }

    private void SpawnArrow(BowSO.ShotStats stats, Vector2 dir)
    {
        float spread = Random.Range(-stats.spreadDeg, stats.spreadDeg);
        dir = (Quaternion.Euler(0, 0, spread) * (Vector3)dir).normalized;

        Vector3 spawnPos;
        if (_muzzle != null)
            spawnPos = _muzzle.position;
        else
            spawnPos = transform.position + (Vector3)(dir * spawnOffsetFromCenter);

        var prefabProj = _bow.arrowPrefab;
        if (prefabProj == null)
            return;

        var manager = _poolManager != null ? _poolManager : GameplayPoolManager.Instance;

        Projectile projBase = manager
            ? manager.SpawnProjectile(prefabProj, spawnPos, Quaternion.identity)
            : Instantiate(prefabProj, spawnPos, Quaternion.identity);

        var arrow = projBase as ArrowProjectile;
        if (arrow == null)
            return;

        arrow.Initialize(dir, stats.speed, stats.damage, _bow.arrowLifetime, _ownerCollider);
    }

    private Vector2 GetAimDirection()
    {
        var cam = Camera.main;
        if (!cam) return Vector2.right;

        Vector2 screen;
        if (Pointer.current != null)
            screen = Pointer.current.position.ReadValue();
        else if (Mouse.current != null)
            screen = Mouse.current.position.ReadValue();
        else
            return Vector2.right;

        Vector3 depthRef = _muzzle ? _muzzle.position : transform.position;
        float depth = cam.orthographic ? 0f : Mathf.Abs(cam.WorldToScreenPoint(depthRef).z);

        Vector3 world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, depth));
        world.z = depthRef.z;

        Vector2 origin = _muzzle ? (Vector2)_muzzle.position : (Vector2)transform.position;
        Vector2 v = (Vector2)(world - (Vector3)origin);
        return v.sqrMagnitude > 0.0001f ? v.normalized : Vector2.right;
    }

    public void FireMultiShotVolley(BowSO.ShotStats stats, int arrowCount, float totalSpreadDegrees)
    {
        BowSO.ShotStats finalStats = ApplyResolvedDamageModifiers(stats);

        Vector2 baseDir = GetAimDirection();
        if (baseDir.sqrMagnitude < 0.0001f)
            baseDir = Vector2.right;

        _playerFacing?.SetFacing(baseDir);

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
        const float minDamageMultiplier = 0f;

        float outgoingDamageMultiplier = 1f;

        if (_stats != null)
        {
            outgoingDamageMultiplier = Mathf.Max(
                minDamageMultiplier,
                _stats.ResolvedEffects.outgoingDamageMultiplier);
        }

        stats.damage *= outgoingDamageMultiplier;
        return stats;
    }
}