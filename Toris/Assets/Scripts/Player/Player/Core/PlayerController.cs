using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private PlayerInputReader _inputReader;
    [SerializeField] private PlayerMotor _motor;
    [SerializeField] private PlayerAnimationController _animController;
    [SerializeField] private PlayerStats _stats;
    [SerializeField] private DashConfig _dash;

    void OnEnable() { if (_inputReader) _inputReader.OnDashPressed += HandleDashRequested; }
    void OnDisable() { if (_inputReader) _inputReader.OnDashPressed -= HandleDashRequested; }


    void Update()
    {
        if (!_inputReader || !_motor || !_animController)
            return;

        // Read movement input
        Vector2 move = _inputReader.Move;

        // Ask animation FSM if movement is allowed
        bool canMove = _animController.CanMove();
        _motor.SetMovementLocked(!canMove);

        // Always send the input to motor (it decides zero or not)
        _motor.SetMoveInput(move);

        var animVec = _motor.IsDashing ? Vector2.zero : move;
        _animController.Tick(animVec);
    }

    private Vector2 _dashFacing;

    private void HandleDashRequested()
    {
        if (!_motor || !_stats || !_dash) return;

        // Determine facing
        Vector2 input = _inputReader.Move;
        Vector2 facing = input.sqrMagnitude > 0.01f ? input : _animController.CurrentFacing;
        if (facing.sqrMagnitude < 0.0001f)
            facing = _dashFacing == Vector2.zero ? Vector2.right : _dashFacing;

        // Only pay stamina if the dash will start
        if (_stats.currentStamina < _dash.staminaCost) return;        // quick precheck
        if (_motor.TryStartDash(facing.normalized))
        {
            _dashFacing = facing.normalized;
            _stats.TryConsumeStamina(_dash.staminaCost);              // now spend it
        }
    }
}
