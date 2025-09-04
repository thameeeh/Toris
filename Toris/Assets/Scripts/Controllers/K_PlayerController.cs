using UnityEngine;
using UnityEngine.InputSystem;

public class K_PlayerController : MonoBehaviour
{
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] InputActionReference moveAction;

    // NEW:
    [SerializeField] PlayerAnimationView animView;
    [SerializeField] InputActionReference fireAction; // add "Fire" in your input asset (Mouse LMB + Space)

    Rigidbody2D rb;
    Vector2 input;

    void Awake() { rb = GetComponent<Rigidbody2D>(); }

    void OnEnable() {
        moveAction.action.Enable();
        if (fireAction != null) fireAction.action.Enable();
    }
    void OnDisable() {
        moveAction.action.Disable();
        if (fireAction != null) fireAction.action.Disable();
    }

    void Update()
    {
        input = moveAction.action.ReadValue<Vector2>();
        if (input.sqrMagnitude > 1f) input = input.normalized;

        // drive animations
        if (animView) animView.Tick(input);

        // temp: trigger Shoot anim to test (bullets later)
        if (fireAction && fireAction.action.triggered)
            animView.PlayShoot();
    }

    void FixedUpdate() { rb.linearVelocity = input * moveSpeed; }
}
