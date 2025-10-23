using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMotor : MonoBehaviour
{
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private PlayerMoveConfig config;
    [SerializeField] private DashAbility _dashAbility = new DashAbility();

    private Vector2 _moveInput;
    private bool _movementLocked;

    public DashAbility DashAbility => _dashAbility;
    public bool isDashing => _dashAbility != null && _dashAbility.isActive;

    void Awake()
    {
        if (!rb)
            rb = GetComponent<Rigidbody2D>();
        _dashAbility?.Initialize(rb, config, ApplyVelocity);
    }

    void OnValidate()
    {
        if (!rb)
            rb = GetComponent<Rigidbody2D>();
        _dashAbility?.Initialize(rb, config, ApplyVelocity);
    }

    public void SetMoveInput(Vector2 v) => _moveInput = v;

    public void SetMovementLocked(bool locked)
    {
        _movementLocked = locked;
        if (locked && rb && !isDashing)
        {
            ApplyVelocity(Vector2.zero);
        }
    }

    public bool TryStartDash(Vector2 direction)
    {
        if (_dashAbility == null)
        { 
            return false; 
        }
        return _dashAbility.TryActivate(direction);
    }

    void Reset() => rb = GetComponent<Rigidbody2D>();

    void FixedUpdate()
    {
        if (!rb || !config) return;

        if (_dashAbility != null)
        {
            _dashAbility.FixedTick(Time.fixedDeltaTime);
            if (_dashAbility.isActive)
                return;
        }

        Vector2 dir = _movementLocked ? Vector2.zero : _moveInput;

        if (config.clampDiagonal && dir.sqrMagnitude > 1f)
            dir.Normalize();

        if (config.rotateInput45)
        {
            float s = 0.70710678f;
            dir = new Vector2(dir.x * s - dir.y * s, dir.x * s + dir.y * s);
        }

        ApplyVelocity(dir * config.speed);
    }
    private void ApplyVelocity(Vector2 velocity)
    {
#if UNITY_2022_1_OR_NEWER
        rb.linearVelocity = velocity;
#else
        rb.velocity = velocity;
#endif
    }
}
