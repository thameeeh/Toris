using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private const float MOVE_INPUT_EPSILON_SQR = 0.01f;
    private const float FACING_EPSILON_SQR = 0.0001f;

    [Header("References")]
    [SerializeField] private PlayerInputReaderSO _inputReader;
    [SerializeField] private PlayerMotor _motor;
    [SerializeField] private PlayerStats _stats;
    [SerializeField] private PlayerFacing _playerFacing;

    private Vector2 _lastDashFacing = Vector2.right;

    public DashAbility DashAbility => _motor != null ? _motor.DashAbility : null;

    private void OnEnable()
    {
        if (_inputReader != null)
        {
            _inputReader.OnDashPressed += HandleDashRequested;
        }
    }

    private void OnDisable()
    {
        if (_inputReader != null)
        {
            _inputReader.OnDashPressed -= HandleDashRequested;
        }
    }

    private void OnValidate()
    {
        if (_inputReader == null)
        {
            Debug.LogError($"<b><color=red>[PlayerController]</color></b> is missing PlayerInputReaderSO on GameObject: <b>{name}</b>", this);
        }

        if (_motor == null)
        {
            Debug.LogError($"<b><color=red>[PlayerController]</color></b> is missing PlayerMotor on GameObject: <b>{name}</b>", this);
        }

        if (_stats == null)
        {
            Debug.LogError($"<b><color=red>[PlayerController]</color></b> is missing PlayerStats on GameObject: <b>{name}</b>", this);
        }

        if (_playerFacing == null)
        {
            Debug.LogWarning($"[PlayerController] Missing PlayerFacing on {name}", this);
        }
    }

    private void Update()
    {
        if (_inputReader == null || _motor == null)
            return;

        Vector2 moveInput = _inputReader.Move;
        _motor.SetMoveInput(moveInput);

        if (moveInput.sqrMagnitude > MOVE_INPUT_EPSILON_SQR)
        {
            _playerFacing?.SetFacing(moveInput);
        }
    }

    private void HandleDashRequested()
    {
        if (_inputReader == null || _motor == null || _stats == null)
            return;

        DashAbility dashAbility = _motor.DashAbility;
        DashConfig dashConfig = dashAbility != null ? dashAbility.Config : null;

        if (dashConfig == null)
            return;

        Vector2 dashFacing = ResolveDashFacing();

        if (dashFacing.sqrMagnitude < FACING_EPSILON_SQR)
            return;

        if (_stats.currentStamina < dashConfig.staminaCost)
            return;

        Vector2 normalizedDashFacing = dashFacing.normalized;

        if (_motor.TryStartDash(normalizedDashFacing))
        {
            _lastDashFacing = normalizedDashFacing;
            _playerFacing?.SetFacing(normalizedDashFacing);
            _stats.TryConsumeStamina(dashConfig.staminaCost);
        }
    }

    private Vector2 ResolveDashFacing()
    {
        Vector2 inputMove = _inputReader != null ? _inputReader.Move : Vector2.zero;

        if (inputMove.sqrMagnitude > MOVE_INPUT_EPSILON_SQR)
            return inputMove;

        if (_playerFacing != null && _playerFacing.CurrentFacing.sqrMagnitude > FACING_EPSILON_SQR)
            return _playerFacing.CurrentFacing;

        return _lastDashFacing;
    }
}