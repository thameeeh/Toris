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
        // Safety: never leave the player locked if this gets disabled
        motor?.SetMovementLocked(false);
        drawing = false;
    }

    void BeginDraw()
    {
        if (Time.time - lastShotTime < bow.cooldownAfterShot) return;

        drawing = true;
        drawStartTime = Time.time;
        motor?.SetMovementLocked(true);

        // Face mouse immediately
        var aim = GetAimDirection();
        if (aim.sqrMagnitude > 0.0001f) anim.UpdateShootFacing(aim);

        anim.BeginBowHold();
    }

    void ReleaseShot()
    {
        if (!drawing)
        {
            motor?.SetMovementLocked(false);
            return;
        }

        drawing = false;

        float held = Time.time - drawStartTime;

        // Always unlock movement when the hold ends
        motor?.SetMovementLocked(false);

        // If released before nock, dry release (no arrow)
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
        if (!bow.arrowPrefab || !muzzle) return;

        Vector2 dir = GetAimDirection();
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;

        float spread = Random.Range(-stats.spreadDeg, stats.spreadDeg);
        dir = (Quaternion.Euler(0, 0, spread) * (Vector3)dir).normalized;

        var rb = Instantiate(bow.arrowPrefab, muzzle.position, Quaternion.identity);

        var proj = rb.GetComponent<ArrowProjectile>();
        if (proj == null) proj = rb.gameObject.AddComponent<ArrowProjectile>();

        proj.Initialize(dir, stats.speed, stats.damage, bow.arrowLifetime, ownerCollider);
    }

    private Vector2 GetAimDirection()
    {
        var cam = Camera.main;
        if (!cam || muzzle == null)
            return Vector2.right;

        // Read pointer position from the new Input System (mouse or touch/pen)
        Vector2 screen;
        if (Pointer.current != null)
            screen = Pointer.current.position.ReadValue();
        else if (Mouse.current != null)
            screen = Mouse.current.position.ReadValue();
        else
            return Vector2.right; // no pointer available (e.g., only gamepad)

        // Convert to world. For perspective, use the muzzle depth so the ray lands on the same Z.
        float depth = cam.orthographic
            ? 0f
            : Mathf.Abs(cam.WorldToScreenPoint(muzzle.position).z);

        Vector3 world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, depth));
        world.z = muzzle.position.z;

        Vector2 dir = ((Vector2)(world - muzzle.position)).normalized;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
        return dir;
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
