using UnityEngine;
using UnityEngine.InputSystem;

public class K_PlayerController : MonoBehaviour
{
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] InputActionReference moveAction;

    Rigidbody2D rb;
    Vector2 input;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void OnEnable() => moveAction.action.Enable();
    void OnDisable() => moveAction.action.Disable();

    void Update()
    {
        input = moveAction.action.ReadValue<Vector2>();
        if (input.sqrMagnitude > 1f) input = input.normalized;
    }

    void FixedUpdate()
    {
        rb.linearVelocity = input * moveSpeed;
    }
}
