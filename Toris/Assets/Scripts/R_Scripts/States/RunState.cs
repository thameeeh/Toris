using UnityEngine;

public class RunState : State
{
    public override void Enter()
    {
        isComplete = false;
        current = _isSouth;

        //play animation based on direction
        if (_isSouth) animator.Play("Run_S");
        else          animator.Play("Run_N");
    }
    public override void Do()
    {
        if (!input.IsRunning || input.Speed != 3)
        {
            isComplete = true;
        }

        //to avoid restarting the animation every frame
        if (current != _isSouth)
        {
            current = _isSouth;
            if (_isSouth) animator.Play("Run_S");
            else          animator.Play("Run_N");
        }
    }
    public override void FixedDo()
    {
    }
    public override void Exit()
    {
    }
}
