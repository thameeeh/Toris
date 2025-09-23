using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private PlayerInputReader _inputReader;
    [SerializeField] private PlayerMotor _motor;
    [SerializeField] private PlayerAnimationController _animController;

    void Update()
    {
        var move = _inputReader ? _inputReader.Move : Vector2.zero;

        _motor.SetMoveInput(move);
        if (_animController) _animController.Tick(move);
    }
}
