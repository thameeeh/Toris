using UnityEngine;

public class BadgerIdleState : EnemyState<Badger>
{
    public BadgerIdleState(Badger enemy, EnemyStateMachine enemyStateMachine) 
        : base(enemy, enemyStateMachine) { }

    public override void EnterState()
    {
        base.EnterState();

        enemy.BadgerIdleBaseInstance.DoEnterLogic();
    }

    public override void ExitState()
    {
        base.ExitState();

        enemy.BadgerIdleBaseInstance.DoExitLogic();
    }

    public override void FrameUpdate()
    {
        base.FrameUpdate();
        
        if(enemy.isWondering) enemy.StateMachine.ChangeState(enemy.WalkState);

        //--------  Is Aggroed Starts burrowing  --------//
        if (enemy.IsAggroed && enemy.ForcedIdleDuration <= 0)
        {
            enemy.StateMachine.ChangeState(enemy.BurrowState);
        }

        enemy.BadgerIdleBaseInstance.DoFrameUpdateLogic();
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        enemy.BadgerIdleBaseInstance.DoPhysicsLogic();
    }

    public override void AnimationTriggerEvent(Enemy.AnimationTriggerType triggerType)
    {
        base.AnimationTriggerEvent(triggerType);

        enemy.BadgerIdleBaseInstance.DoAnimationTriggerEventLogic(triggerType);
    }
}
