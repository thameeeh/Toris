using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private PlayerInputReaderSO _inputReader;
    [SerializeField] private PlayerMotor _motor;
    [SerializeField] private PlayerAnimationController _animController;
    [SerializeField] private PlayerStats _stats;
    [SerializeField] private DashConfig _dash;

    void OnEnable() { if (_inputReader) _inputReader.OnDashPressed += HandleDashRequested; }
    void OnDisable() { if (_inputReader) _inputReader.OnDashPressed -= HandleDashRequested; }
    private void OnValidate()
    {
        if (_inputReader == null)
        {
            Debug.LogError($"<b><color=red>[PlayerController]</color></b> is missing PlayerInputReaderSO on GameObject: <b>{name}<b>", this);
        }
    }
    public DashAbility DashAbility => _motor != null ? _motor.DashAbility : null;
    void Update()
    {
        if (!_inputReader || !_motor || !_animController)
            return;

        Vector2 move = _inputReader.Move;

        bool canMove = _animController.CanMove();
        _motor.SetMovementLocked(!canMove);

        _motor.SetMoveInput(move);

        var animVec = _motor.isDashing ? Vector2.zero : move;
        _animController.Tick(animVec);
    }

    private Vector2 _dashFacing;

    private void HandleDashRequested()
    {
        if (!_motor || !_stats || !_animController) return;

        DashAbility dashAbility = _motor.DashAbility;
        DashConfig dashConfig = dashAbility?.Config;
        if (dashConfig == null)
            return;

        Vector2 input = _inputReader ? _inputReader.Move : Vector2.zero;
        Vector2 facing = input.sqrMagnitude > 0.01f ? input : _animController.CurrentFacing;
        if (facing.sqrMagnitude < 0.0001f)
            facing = _dashFacing == Vector2.zero ? Vector2.right : _dashFacing;

        if (_stats.currentStamina < dashConfig.staminaCost)
            return;

        if (_motor.TryStartDash(facing.normalized))
        {
            _dashFacing = facing.normalized;
            _stats.TryConsumeStamina(dashConfig.staminaCost);
        }
    }
}
