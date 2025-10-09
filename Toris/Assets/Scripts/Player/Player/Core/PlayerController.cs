using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private PlayerInputReader _inputReader;
    [SerializeField] private PlayerMotor _motor;
    [SerializeField] private PlayerAnimationController _animController;

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

        // Drive animation FSM
        _animController.Tick(move);
    }
}
