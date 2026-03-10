using UnityEngine;

public class WolfReturnHomeState : EnemyState<Wolf>
{
    public WolfReturnHomeState(Wolf enemy, EnemyStateMachine enemyStateMachine)
        : base(enemy, enemyStateMachine)
    {
    }

    public override void EnterState()
    {
        enemy.EnemyReturnHomeBaseInstance.DoEnterLogic();
    }

    public override void ExitState()
    {
        enemy.EnemyReturnHomeBaseInstance.DoExitLogic();
    }

    public override void FrameUpdate()
    {
        enemy.EnemyReturnHomeBaseInstance.DoFrameUpdateLogic();

        if (enemy.IsAggroed)
        {
            enemyStateMachine.ChangeState(enemy.HowlState);
            return;
        }

        if (enemy.EnemyReturnHomeBaseInstance.HasArrived)
        {
            enemyStateMachine.ChangeState(enemy.IdleState);
        }
    }

    public override void PhysicsUpdate()
    {
        enemy.EnemyReturnHomeBaseInstance.DoPhysicsLogic();
    }

    public override void AnimationTriggerEvent(Enemy.AnimationTriggerType triggerType)
    {
        enemy.EnemyReturnHomeBaseInstance.DoAnimationTriggerEventLogic(triggerType);
    }
}