using UnityEngine;
using UnityEngine.InputSystem;

public class K_PlayerController : MonoBehaviour
{
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] InputActionReference moveAction;
    [SerializeField] PlayerAnimationView animView;
    [SerializeField] InputActionReference fireAction;

    [Header("Shooting")]
    [SerializeField] bool lockMovementWhileShooting = true;
    [SerializeField] float shootLockDuration = 0.20f;
    [SerializeField] float moveWhileShootingMult = 0.4f;
    [SerializeField] K_Arrow arrowPrefab;
    [SerializeField] Transform firePoint;
    [SerializeField] float fireCooldown = 0.12f;

    float nextFireTime;
    float shootLockUntil;

    Rigidbody2D rb;
    Vector2 input;
    Camera cam;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        cam = Camera.main;
    }

    void OnEnable()
    {
        moveAction.action.Enable();
        fireAction.action.Enable();

        // prefer started so it triggers on press immediately
        fireAction.action.started += OnFire;
        // you can also listen to performed if you want release behavior
        // fireAction.action.performed += OnFire;

        Debug.Log("[Player] OnEnable: actions enabled");
    }
    void OnDisable()
    {
        fireAction.action.started -= OnFire;
        // fireAction.action.performed -= OnFire;

        fireAction.action.Disable();
        moveAction.action.Disable();
    }

    void OnFire(InputAction.CallbackContext ctx)
    {
        Debug.Log("[Player] OnFire event!");

        if (Time.time < nextFireTime) { Debug.Log("[Player] Fire gated by cooldown"); return; }
        nextFireTime = Time.time + fireCooldown;

        var dir = GetLookDir();
        if (dir.sqrMagnitude < 0.0001f) { Debug.LogWarning("[Player] GetLookDir zero"); return; }

        if (animView) { animView.SetFacing(dir); animView.PlayShoot(); }
        shootLockUntil = Time.time + shootLockDuration;

        SpawnArrow(dir);
    }

    void Update()
    {
        // TEMP: fallback for testing if your InputAction isn’t hooked yet
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Debug.Log("[Player] Fallback click fired");
            OnFire(new InputAction.CallbackContext());
        }
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Debug.Log("[Player] Fallback SPACE fired");
            OnFire(new InputAction.CallbackContext());
        }

        // movement input
        input = moveAction.action.ReadValue<Vector2>();
        if (input.sqrMagnitude > 1f) input = input.normalized;

        // movement lock/slow while shooting
        Vector2 moveVec = input;
        if (Time.time < shootLockUntil)
            moveVec = lockMovementWhileShooting ? Vector2.zero : moveVec * moveWhileShootingMult;

        // anim direction: only follow mouse while fire is held
        Vector2 animDir = (fireAction.action != null && fireAction.action.IsPressed())
            ? GetLookDir()
            : moveVec;

        if (animView) animView.Tick(animDir);

        input = moveVec;
    }

    void FixedUpdate()
    {
        // Use velocity on Rigidbody2D
        rb.linearVelocity = input * moveSpeed;
    }

    Vector2 GetLookDir()
    {
        if (!cam) cam = Camera.main;
        if (Mouse.current == null) return Vector2.zero;

        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        // z must be camera-to-plane distance; 0 works for orthographic cams
        Vector3 mouseWorld = cam.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, 0f));
        mouseWorld.z = 0f;
        return ((Vector2)(mouseWorld - transform.position)).normalized;
    }

    void SpawnArrow(Vector2 dir)
    {
        if (!arrowPrefab)
        {
            Debug.LogWarning("[Player] No arrowPrefab assigned on PlayerController!");
            return;
        }

        Vector3 spawnPos = firePoint ? firePoint.position : transform.position;
        var arrow = Instantiate(arrowPrefab, spawnPos, Quaternion.identity);
        arrow.Init(dir, gameObject); // make sure K_Arrow has this method
        Debug.Log("[Player] Arrow spawned");
    }
}
