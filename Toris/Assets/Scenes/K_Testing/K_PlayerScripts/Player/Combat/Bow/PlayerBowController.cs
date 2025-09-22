using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerBowController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerInputReader input;
    [SerializeField] private PlayerAnimationView anim;
    [SerializeField] private BowSO bow;
    [SerializeField] private Transform muzzle;
    [SerializeField] private PlayerMotor motor;
    [SerializeField] private Collider2D ownerCollider;
    [SerializeField] private ProjectilePoolRegistry pool; // NEW: pooled spawns

    [Header("Spawn Fallback")]
    [Tooltip("Used if muzzle is null. Arrow spawns this far from player along aim.")]
    [SerializeField] private float spawnOffsetFromCenter = 0.35f;

    private float drawStartTime = -999f;
    private float lastShotTime = -999f;
    private bool drawing;

    void OnEnable()
    {
        if (input != null)
        {
            input.OnShootStarted += BeginDraw;
            input.OnShootReleased += ReleaseShot;
        }
    }

    void OnDisable()
    {
        if (input != null)
        {
            input.OnShootStarted -= BeginDraw;
            input.OnShootReleased -= ReleaseShot;
        }
        motor?.SetMovementLocked(false);
        drawing = false;
    }

    void BeginDraw()
    {
        if (bow == null) return;
        if (Time.time - lastShotTime < bow.cooldownAfterShot) return;

        drawing = true;
        drawStartTime = Time.time;
        motor?.SetMovementLocked(true);

        var aim = GetAimDirection();
        if (aim.sqrMagnitude > 0.0001f) anim?.UpdateShootFacing(aim);

        anim?.BeginBowHold();
    }

    void ReleaseShot()
    {
        if (!drawing)
        {
            motor?.SetMovementLocked(false);
            return;
        }

        drawing = false;
        motor?.SetMovementLocked(false);

        if (bow == null) return;

        float held = Time.time - drawStartTime;

        // Dry release
        if (held < bow.nockTime)
        {
            anim?.ReleaseBowAndFinish();
            lastShotTime = Time.time;
            return;
        }

        float overHoldExtra = Mathf.Max(0f, held - bow.overHoldStartsAt);
        var stats = bow.BuildShotStats(held, overHoldExtra);

        anim?.ReleaseBowAndFinish();
        FireArrow(stats);

        lastShotTime = Time.time;
    }

    void FireArrow(BowSO.ShotStats stats)
    {
        if (bow == null) return;
        if (bow.arrowPrefab == null) return;

        Vector2 dir = GetAimDirection();
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;

        // Apply random spread
        float spread = Random.Range(-stats.spreadDeg, stats.spreadDeg);
        dir = (Quaternion.Euler(0, 0, spread) * (Vector3)dir).normalized;

        // Determine spawn position
        Vector3 spawnPos;
        if (muzzle != null)
        {
            spawnPos = muzzle.position;
        }
        else
        {
            // Spawn a bit in front of the player along aim
            spawnPos = transform.position + (Vector3)(dir * spawnOffsetFromCenter);
        }

        // Need an ArrowProjectile prefab to work with the pool
        var prefabAP = bow.arrowPrefab.GetComponent<ArrowProjectile>();
        if (prefabAP == null)
        {
            Debug.LogError("BowSO.arrowPrefab must have ArrowProjectile on it.");
            return;
        }

        ArrowProjectile proj = pool
            ? pool.Spawn(prefabAP, spawnPos, Quaternion.identity)
            : Instantiate(prefabAP, spawnPos, Quaternion.identity);

        proj.Initialize(dir, stats.speed, stats.damage, bow.arrowLifetime, ownerCollider);
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
        Vector3 depthRef = muzzle ? muzzle.position : transform.position;
        float depth = cam.orthographic ? 0f : Mathf.Abs(cam.WorldToScreenPoint(depthRef).z);

        Vector3 world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, depth));
        world.z = depthRef.z;

        Vector2 origin = muzzle ? (Vector2)muzzle.position : (Vector2)transform.position;
        Vector2 v = (Vector2)(world - (Vector3)origin);
        return v.sqrMagnitude > 0.0001f ? v.normalized : Vector2.right;
    }

    void Update()
    {
        if (drawing && anim != null)
        {
            Vector2 aim = GetAimDirection();
            if (aim.sqrMagnitude > 0.0001f)
                anim.UpdateShootFacing(aim);
        }
    }
}
