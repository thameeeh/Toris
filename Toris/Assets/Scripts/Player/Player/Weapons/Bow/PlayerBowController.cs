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
    private float lastShotTime = -999f;
    private bool drawing;
    public bool IsAutoaiming { get; set; } = false;

    void OnEnable()
    {
        if (_input != null)
        {
            _input.OnShootStarted += BeginDraw;
            _input.OnShootReleased += ReleaseShot;
        }
    }

    void OnDisable()
    {
        if (_input != null)
        {
            _input.OnShootStarted -= BeginDraw;
            _input.OnShootReleased -= ReleaseShot;
        }
        _motor?.SetMovementLocked(false);
        drawing = false;
    }

    void BeginDraw()
    {
        if (_bow == null) return;
        if (Time.time - lastShotTime < _bow.cooldownAfterShot) return;

        drawing = true;
        drawStartTime = Time.time;
        _motor?.SetMovementLocked(true);

        var aim = GetAimDirection();
        if (aim.sqrMagnitude > 0.0001f) _animController?.UpdateAim(aim);

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

        // Dry release
        if (held < _bow.nockTime)
        {
            _animController?.ReleaseHold();
            lastShotTime = Time.time;
            return;
        }

        float overHoldExtra = Mathf.Max(0f, held - _bow.overHoldStartsAt);
        var stats = _bow.BuildShotStats(held, overHoldExtra);

        _animController?.ReleaseHold();
        FireArrow(stats);

        lastShotTime = Time.time;
    }

    void FireArrow(BowSO.ShotStats stats)
    {
        if (_bow == null) return;
        if (_bow.arrowPrefab == null) return;

        Vector2 dir = GetAimDirection();
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;

        // Apply random spread
        float spread = Random.Range(-stats.spreadDeg, stats.spreadDeg);
        dir = (Quaternion.Euler(0, 0, spread) * (Vector3)dir).normalized;

        // Determine spawn position
        Vector3 spawnPos;
        if (_muzzle != null)
        {
            spawnPos = _muzzle.position;
        }
        else
        {
            // Spawn a bit in front of the player along aim
            spawnPos = transform.position + (Vector3)(dir * spawnOffsetFromCenter);
        }

        // Get a Projectile prefab from the Bow (ArrowProjectile derives from Projectile)
        var prefabProj = _bow.arrowPrefab.GetComponent<Projectile>();
        if (prefabProj == null)
        {
            Debug.LogError("BowSO.arrowPrefab must have a Projectile-derived component on it.");
            return;
        }

        var manager = _poolManager != null ? _poolManager : GameplayPoolManager.Instance;

        // Use the generalized projectile spawn API
        Projectile projBase = manager
            ? manager.SpawnProjectile(prefabProj, spawnPos, Quaternion.identity)
            : Instantiate(prefabProj, spawnPos, Quaternion.identity);

        // We still expect this particular bow to fire ArrowProjectiles
        var arrow = projBase as ArrowProjectile;
        if (arrow == null)
        {
            Debug.LogError("Spawned projectile is not an ArrowProjectile; cannot Initialize arrow-specific data.");
            return;
        }

        arrow.Initialize(dir, stats.speed, stats.damage, _bow.arrowLifetime, _ownerCollider);
    }

    private Vector2 GetAimDirection()
    {
        var cam = Camera.main;
        if (!cam) return Vector2.right;

        // New Input System
        Vector2 screen;
        if (Pointer.current != null)
            screen = Pointer.current.position.ReadValue();
        else if (Mouse.current != null)
            screen = Mouse.current.position.ReadValue();
        else
            return Vector2.right; // no pointer device

        // Use muzzle (if present) as depth reference; else player position
        Vector3 depthRef = _muzzle ? _muzzle.position : transform.position;
        float depth = cam.orthographic ? 0f : Mathf.Abs(cam.WorldToScreenPoint(depthRef).z);

        Vector3 world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, depth));
        world.z = depthRef.z;

        Vector2 origin = _muzzle ? (Vector2)_muzzle.position : (Vector2)transform.position;
        Vector2 v = (Vector2)(world - (Vector3)origin);
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
}
