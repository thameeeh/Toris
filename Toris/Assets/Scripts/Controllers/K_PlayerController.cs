using UnityEngine;
using UnityEngine.InputSystem;

public class K_PlayerController : MonoBehaviour
{
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] InputActionReference moveAction;
    [SerializeField] PlayerAnimationView animView;
    [SerializeField] InputActionReference fireAction;
    [SerializeField] bool lockMovementWhileShooting = true;
    [SerializeField] float shootLockDuration = 0.20f;
    [SerializeField] float moveWhileShootingMult = 0.4f;
    float shootLockUntil;

    Rigidbody2D rb;
    Vector2 input;

    void Awake() { rb = GetComponent<Rigidbody2D>(); }

    void OnEnable()
    {
        moveAction.action.Enable();
        fireAction.action.Enable();
        fireAction.action.performed += OnFire;
    }
    void OnDisable()
    {
        fireAction.action.performed -= OnFire;
        fireAction.action.Disable();
        moveAction.action.Disable();
    }
    void OnFire(InputAction.CallbackContext _)
    {
        if (animView) animView.PlayShoot();
        shootLockUntil = Time.time + shootLockDuration;
        // bullet goes here later
    }
    void Update()
    {
        input = moveAction.action.ReadValue<Vector2>();
        if (input.sqrMagnitude > 1f) input = input.normalized;

        if (Time.time < shootLockUntil)
            input = lockMovementWhileShooting ? Vector2.zero : input * moveWhileShootingMult;
        // drive animations
        if (animView) animView.Tick(input);

        // // temp: trigger Shoot anim to test (bullets later)
        // if (fireAction && fireAction.action.triggered)
        //     animView.PlayShoot();
    }

    void FixedUpdate() { rb.linearVelocity = input * moveSpeed; }
}




