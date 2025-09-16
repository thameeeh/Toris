using UnityEngine;

public class WalkState : State
{

    public override void Enter()
    {
        animator.Play("Walk_N");
        animator.SetFloat("Speed", 1f);
        input.speed = 1f;
    }
    public override void Do()
    {
        if(input.isRunning)
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
