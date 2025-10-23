using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMotor : MonoBehaviour
{
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private PlayerMoveConfig config;
    [SerializeField] private DashConfig _dash;

    private Vector2 _moveInput;
    private bool _movementLocked;

    private float _dashCooldownTimer;
    private float _dashTimer;
    private Vector2 _dashDirection;

    public bool IsDashing => _dashTimer > 0f;

    public void SetMoveInput(Vector2 v) => _moveInput = v;

    public void SetMovementLocked(bool locked)
    {
        _movementLocked = locked;
        if (locked && rb && _dashTimer <= 0f)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    public bool TryStartDash(Vector2 direction)
    {
        if (_dashTimer > 0f) return false;
        if (_dashCooldownTimer > 0f) return false;
        if (direction.sqrMagnitude < 0.01f) return false;

        _dashDirection = direction.normalized;
        _dashTimer = _dash.duration;
        return true;
    }

    void Reset() => rb = GetComponent<Rigidbody2D>();

    void FixedUpdate()
    {
        if (!rb || !config) return;

        if (_dashTimer > 0f)
        {
            float temp = 1f - (_dashTimer / _dash.duration);

            float speedBlend = Mathf.Lerp(_dash.initialSpeed, config.speed, temp);

            float shaped = speedBlend * _dash.speedCurve.Evaluate(temp);

            rb.linearVelocity = _dashDirection * shaped;
            _dashTimer = Mathf.Max(0f, _dashTimer - Time.fixedDeltaTime);

            if (_dashTimer == 0f)
                _dashCooldownTimer = _dash.cooldown;

            return;
        }

        if (_dashCooldownTimer > 0f)
        {
            _dashCooldownTimer = Mathf.Max(0f, _dashCooldownTimer - Time.fixedDeltaTime);
        }

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
