using UnityEngine;

public class RunState : State
{
    public override void Enter()
    {
        animator.Play("Run_S");
        animator.SetFloat("Speed", 3f);
        input.speed = 3f;
    }
    public override void Do()
    {
        if (!input.isRunning)
        {
            isComplete = true;
        }
        else if (input._input.magnitude == 0)
        {
            isComplete = true;
        }
    }
    public override void FixedDo()
    {
    }
    public override void Exit()
    {
    }
}
