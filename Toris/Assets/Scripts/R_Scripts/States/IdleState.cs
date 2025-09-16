using UnityEngine;

public class IdleState : State
{
    public override void Enter()
    {
        animator.Play("Idle_N");
    }
    public override void Do()
    {
        if (input._input.magnitude > 0)
        {
            isComplete = true;
        }
        animator.SetFloat("Speed", 0f);
    }
    public override void FixedDo()
    {
    }
    public override void Exit()
    {
    }
}
