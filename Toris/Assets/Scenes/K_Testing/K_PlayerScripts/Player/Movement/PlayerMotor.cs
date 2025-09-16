using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMotor : MonoBehaviour
{
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private PlayerMoveConfig config;

    private Vector2 _moveInput;
    private bool _movementLocked;

    public void SetMoveInput(Vector2 v) => _moveInput = v;

    public void SetMovementLocked(bool locked)
    {
        _movementLocked = locked;
        if (locked && rb)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    void Reset() => rb = GetComponent<Rigidbody2D>();

    void FixedUpdate()
    {
        if (!rb || !config) return;

        Vector2 dir = _movementLocked ? Vector2.zero : _moveInput;

        if (config.clampDiagonal && dir.sqrMagnitude > 1f)
            dir.Normalize();

        if (config.rotateInput45)
        {
            float s = 0.70710678f;
            dir = new Vector2(dir.x * s - dir.y * s, dir.x * s + dir.y * s);
        }

        rb.linearVelocity = dir * config.speed;
    }
}
