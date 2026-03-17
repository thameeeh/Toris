using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMotor : MonoBehaviour
{
    private const float MIN_MULTIPLIER = 0f;

    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private PlayerMoveSO _moveSO;
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

        _dashAbility?.Initialize(rb, _moveSO, ApplyVelocity);
    }

    private void OnValidate()
    {
        if (!rb)
            rb = GetComponent<Rigidbody2D>();

        _dashAbility?.Initialize(rb, _moveSO, ApplyVelocity);
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
        if (!rb || !_moveSO)
            return;

        float moveSpeedMultiplier = GetMoveSpeedMultiplier();
        float dashSpeedMultiplier = GetDashSpeedMultiplier();

        if (_dashAbility != null)
        {
            _dashAbility.FixedTick(Time.fixedDeltaTime, dashSpeedMultiplier);

            if (_dashAbility.isActive)
                return;
        }

        Vector2 direction = _movementLocked ? Vector2.zero : _moveInput;

        float finalMoveSpeed = _moveSO.speed * moveSpeedMultiplier;
        ApplyVelocity(direction * finalMoveSpeed);
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

    private void ApplyVelocity(Vector2 velocity)
    {
#if UNITY_2022_1_OR_NEWER
        rb.linearVelocity = velocity;
#else
        rb.velocity = velocity;
#endif
    }
}