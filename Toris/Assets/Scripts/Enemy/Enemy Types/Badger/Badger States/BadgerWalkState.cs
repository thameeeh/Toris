using UnityEngine;

public class BadgerWalkState : EnemyState<Badger>
{
    public BadgerWalkState(Badger enemy, EnemyStateMachine enemyStateMachine) 
        : base(enemy, enemyStateMachine) { }

    public override void EnterState()
    {
        base.EnterState();

        enemy.BadgerWalkBaseInstance.DoEnterLogic();
    }

    public override void ExitState()
    {
        base.ExitState();

        enemy.BadgerWalkBaseInstance.DoExitLogic();
    }

    public override void FrameUpdate()
    {
        base.FrameUpdate();

        //--------  Goes Idle  --------//
        if (!enemy.IsWondering) enemy.StateMachine.ChangeState(enemy.IdleState);

        //--------  Is Aggroed Starts burrowing  --------//
        if (enemy.IsAggroed && enemy.ForcedIdelDuration <= 0)
        {
            enemy.StateMachine.ChangeState(enemy.BurrowState);
        }

        enemy.BadgerWalkBaseInstance.DoFrameUpdateLogic();
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        enemy.BadgerWalkBaseInstance.DoPhysicsLogic();
    }
    public override void AnimationTriggerEvent(Enemy.AnimationTriggerType triggerType)
    {
        base.AnimationTriggerEvent(triggerType);

        enemy.BadgerWalkBaseInstance.DoAnimationTriggerEventLogic(triggerType);
    }
}
