using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private PlayerInputReader inputReader;
    [SerializeField] private PlayerMotor2D motor;
    [SerializeField] private PlayerAnimationView animView;

    void Update()
    {
        var move = inputReader ? inputReader.Move : Vector2.zero;

        motor.SetMoveInput(move);
        if (animView) animView.Tick(move);
    }
}
