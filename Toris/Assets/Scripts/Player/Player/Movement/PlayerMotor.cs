using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMotor : MonoBehaviour
{
    private const float MIN_MULTIPLIER = 0f;
    private const float ROTATED_INPUT_SCALE = 0.70710678f;

    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private PlayerMoveConfig config;
    [SerializeField] private PlayerStats _stats;
    [SerializeField] private DashAbility _dashAbility = new DashAbility();

    private Vector2 _moveInput;
    private bool _movementLocked;

    public Vector2 CurrentMoveInput => _moveInput;
    public DashAbility DashAbility => _dashAbility;
    public bool isDashing => _dashAbility != null && _dashAbility.isActive;

    private void Awake()
    {
        if (!rb)
            rb = GetComponent<Rigidbody2D>();

        _dashAbility?.Initialize(rb, config, ApplyVelocity);
    }

    private void OnValidate()
    {
        if (!rb)
            rb = GetComponent<Rigidbody2D>();

        _dashAbility?.Initialize(rb, config, ApplyVelocity);
    }

    public void SetMoveInput(Vector2 moveInput)
    {
        _moveInput = moveInput;
    }

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
            return false;

        return _dashAbility.TryActivate(direction);
    }

    private void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        if (!rb || !config)
            return;

        float moveSpeedMultiplier = GetMoveSpeedMultiplier();
        float dashSpeedMultiplier = GetDashSpeedMultiplier();
        float dashDistanceMultiplier = GetDashDistanceMultiplier();

        if (_dashAbility != null)
        {
            _dashAbility.FixedTick(Time.fixedDeltaTime, dashSpeedMultiplier, dashDistanceMultiplier);

            if (_dashAbility.isActive)
                return;
        }

        Vector2 direction = _movementLocked ? Vector2.zero : _moveInput;

        if (config.clampDiagonal && direction.sqrMagnitude > 1f)
            direction.Normalize();

        if (config.rotateInput45)
        {
            direction = RotateInput45Degrees(direction);
        }

        float finalMoveSpeed = config.speed * moveSpeedMultiplier;
        ApplyVelocity(direction * finalMoveSpeed);
    }

    private Vector2 RotateInput45Degrees(Vector2 direction)
    {
        return new Vector2(
            direction.x * ROTATED_INPUT_SCALE - direction.y * ROTATED_INPUT_SCALE,
            direction.x * ROTATED_INPUT_SCALE + direction.y * ROTATED_INPUT_SCALE);
    }

    private float GetMoveSpeedMultiplier()
    {
        if (_stats == null)
            return 1f;

        return Mathf.Max(MIN_MULTIPLIER, _stats.ResolvedEffects.moveSpeedMultiplier);
    }

    private float GetDashSpeedMultiplier()
    {
        if (_stats == null)
            return 1f;

        return Mathf.Max(MIN_MULTIPLIER, _stats.ResolvedEffects.dashSpeedMultiplier);
    }

    private float GetDashDistanceMultiplier()
    {
        if (_stats == null)
            return 1f;

        return Mathf.Max(MIN_MULTIPLIER, _stats.ResolvedEffects.dashDistanceMultiplier);
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