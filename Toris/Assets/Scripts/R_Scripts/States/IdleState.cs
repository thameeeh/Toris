using UnityEngine;

public class IdleState : State
{
    public override void Enter()
    {
        isComplete = false;
        current = _isSouth;

        //play animation based on direction
        if (_isSouth) animator.Play("Idle_S");
        else          animator.Play("Idle_N");
    }
    public override void Do()
    {
        if (input.MovementVector.magnitude > 0)
        {
            isComplete = true;
        }

        //to avoid restarting the animation every frame
        if (current != _isSouth)
        {
            current = _isSouth;
            if (_isSouth) animator.Play("Idle_S");
            else          animator.Play("Idle_N");
        }
    }
    public override void FixedDo()
    {
    }
    public override void Exit()
    {
    }
}
