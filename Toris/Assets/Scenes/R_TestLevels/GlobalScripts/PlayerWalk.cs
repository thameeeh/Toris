using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerWalk : MonoBehaviour
{
    public InputActionAsset InputActions;

    private InputAction m_moveAction;
    private Vector2 m_moveAmt;
    private Rigidbody2D m_rigidbody;

    public float speed = 5f;

    private void OnEnable()
    {
        InputActions.FindActionMap("Player").Enable();
    }

    private void OnDisable()
    {
        InputActions.FindActionMap("Player").Disable();
    }

    private void Awake()
    {
        // Correct way to find the action
        m_moveAction = InputActions.FindActionMap("Player").FindAction("Move");
        m_rigidbody = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        m_moveAmt = m_moveAction.ReadValue<Vector2>();
    }

    private void FixedUpdate()
    {
        m_rigidbody.MovePosition(
            m_rigidbody.position + m_moveAmt * speed * Time.fixedDeltaTime
        );
    }
}
