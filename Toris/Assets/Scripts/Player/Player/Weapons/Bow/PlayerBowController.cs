using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerBowController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerInputReader _input;
    [SerializeField] private PlayerAnimationController _animController;
    [SerializeField] private BowSO _bow;
    [SerializeField] private Transform _muzzle;
    [SerializeField] private PlayerMotor _motor;
    [SerializeField] private Collider2D _ownerCollider;
    [SerializeField] private GameplayPoolManager _poolManager;

    [Header("Spawn Fallback")]
    [Tooltip("Used if muzzle is null. Arrow spawns this far from player along aim.")]
    [SerializeField] private float spawnOffsetFromCenter = 0.35f;

    private float drawStartTime = -999f;
    private float lastShotTime  = -999f;
    private bool  drawing;
    public bool IsAutoaiming { get; set; } = false;

    // ---- MULTISHOT ----
    private MultiShotConfig _queuedMultiShot;

    void OnEnable()
    {
        if (_input != null)
        {
            _input.OnShootStarted  += BeginDraw;
            _input.OnShootReleased += ReleaseShot;
        }
    }

    void OnDisable()
    {
        if (_input != null)
        {
            _input.OnShootStarted  -= BeginDraw;
            _input.OnShootReleased -= ReleaseShot;
        }
        _motor?.SetMovementLocked(false);
        drawing = false;
    }

    void BeginDraw()
    {
        if (_bow == null) return;
        if (Time.time - lastShotTime < _bow.cooldownAfterShot) return;

        drawing       = true;
        drawStartTime = Time.time;
        _motor?.SetMovementLocked(true);

        var aim = GetAimDirection();
        if (aim.sqrMagnitude > 0.0001f)
            _animController?.UpdateAim(aim);

        _animController?.BeginHold();
    }

    void ReleaseShot()
    {
        if (!drawing)
        {
            _motor?.SetMovementLocked(false);
            return;
        }

        drawing = false;
        _motor?.SetMovementLocked(false);

        if (_bow == null) return;

        float held = Time.time - drawStartTime;

        // Dry release (no arrow)
        if (held < _bow.nockTime)
        {
            _animController?.ReleaseHold();
            lastShotTime = Time.time;
            return;
        }

        float overHoldExtra = Mathf.Max(0f, held - _bow.overHoldStartsAt);
        var   stats         = _bow.BuildShotStats(held, overHoldExtra);

        _animController?.ReleaseHold();
        FireArrow(stats);

        lastShotTime = Time.time;
    }

    void FireArrow(BowSO.ShotStats stats)
    {
        if (_bow == null) return;
        if (_bow.arrowPrefab == null)
        {
            Debug.LogError("[Bow] arrowPrefab is null on BowSO", this);
            return;
        }

        Vector2 baseDir = GetAimDirection();
        if (baseDir.sqrMagnitude < 0.0001f)
            baseDir = Vector2.right;

        // Decide between single shot vs multishot
        if (_queuedMultiShot == null)
        {
            Debug.Log("[Bow] Firing SINGLE arrow", this);
            SpawnArrow(stats, baseDir);
        }
        else
        {
            Debug.Log($"[Bow] Firing MULTISHOT volley: count={_queuedMultiShot.arrowCount}, spread={_queuedMultiShot.totalSpreadDegrees}", this);
            SpawnMultiShotVolley(stats, baseDir, _queuedMultiShot);
            _queuedMultiShot = null; // consume it
        }
    }

    /// <summary>Spawns a single arrow using your original logic.</summary>
    void SpawnArrow(BowSO.ShotStats stats, Vector2 dir)
    {
        // Apply random spread from stats
        float spread = Random.Range(-stats.spreadDeg, stats.spreadDeg);
        dir = (Quaternion.Euler(0, 0, spread) * (Vector3)dir).normalized;

        // Determine spawn position
        Vector3 spawnPos;
        if (_muzzle != null)
            spawnPos = _muzzle.position;
        else
            spawnPos = transform.position + (Vector3)(dir * spawnOffsetFromCenter);

        // Get a Projectile prefab from the Bow (ArrowProjectile derives from Projectile)
        var prefabProj = _bow.arrowPrefab.GetComponent<Projectile>();
        if (prefabProj == null)
        {
            Debug.LogError("[Bow] BowSO.arrowPrefab must have a Projectile-derived component on it.", _bow.arrowPrefab);
            return;
        }

        var manager = _poolManager != null ? _poolManager : GameplayPoolManager.Instance;

        Projectile projBase = manager
            ? manager.SpawnProjectile(prefabProj, spawnPos, Quaternion.identity)
            : Instantiate(prefabProj, spawnPos, Quaternion.identity);

        var arrow = projBase as ArrowProjectile;
        if (arrow == null)
        {
            Debug.LogError("[Bow] Spawned projectile is not an ArrowProjectile; cannot Initialize arrow-specific data.", projBase);
            return;
        }

        Debug.Log($"[Bow] SpawnArrow at {spawnPos}, dir={dir}, speed={stats.speed}, dmg={stats.damage}", arrow);
        arrow.Initialize(dir, stats.speed, stats.damage, _bow.arrowLifetime, _ownerCollider);
    }

    /// <summary>Spawns multiple arrows in a spread pattern around the base direction.</summary>
    void SpawnMultiShotVolley(BowSO.ShotStats stats, Vector2 baseDir, MultiShotConfig config)
    {
        int   count       = Mathf.Max(1, config.arrowCount);
        float totalSpread = config.totalSpreadDegrees;

        for (int i = 0; i < count; i++)
        {
            float t           = (count == 1) ? 0f : (i / (float)(count - 1));
            float angleOffset = Mathf.Lerp(-totalSpread * 0.5f, totalSpread * 0.5f, t);

            Vector2 dir = (Quaternion.Euler(0, 0, angleOffset) * (Vector3)baseDir).normalized;

            // Optional extra randomness from stats
            float random = Random.Range(-stats.spreadDeg, stats.spreadDeg);
            dir = (Quaternion.Euler(0, 0, random) * (Vector3)dir).normalized;

            Debug.Log($"[Bow] MultiShot arrow {i + 1}/{count}, angleOffset={angleOffset}, dir={dir}", this);
            SpawnArrow(stats, dir);
        }
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
        float   depth    = cam.orthographic ? 0f : Mathf.Abs(cam.WorldToScreenPoint(depthRef).z);

        Vector3 world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, depth));
        world.z       = depthRef.z;

        Vector2 origin = _muzzle ? (Vector2)_muzzle.position : (Vector2)transform.position;
        Vector2 v      = (Vector2)(world - (Vector3)origin);
        return v.sqrMagnitude > 0.0001f ? v.normalized : Vector2.right;
    }

    void Update()
    {
        if (drawing && _animController != null)
        {
            Vector2 aim = GetAimDirection();
            if (aim.sqrMagnitude > 0.0001f)
                _animController.UpdateAim(aim);
        }
    }

    // Called by PlayerAbilityController
    public void QueueMultiShot(MultiShotConfig config)
    {
        _queuedMultiShot = config;
        Debug.Log("[Bow] MultiShot QUEUED for next shot", this);
    }
}
