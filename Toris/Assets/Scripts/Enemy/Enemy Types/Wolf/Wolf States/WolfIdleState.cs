using UnityEngine;

public class WolfIdleState : EnemyState<Wolf>
{
    public WolfIdleState(Wolf enemy, EnemyStateMachine enemyStateMachine) 
        : base(enemy, enemyStateMachine) { }

    public override void EnterState()
    {
        base.EnterState();

        enemy.EnemyIdleBaseInstance.DoEnterLogic();
    }

    public override void ExitState()
    {
        base.ExitState();

        enemy.EnemyIdleBaseInstance.DoExitLogic();
    }

    public override void FrameUpdate()
    {
        base.FrameUpdate();

        enemy.EnemyIdleBaseInstance.DoFrameUpdateLogic();
        if (enemy.IsAggroed) 
        {
            enemyStateMachine.ChangeState(enemy.ChaseState);
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        enemy.EnemyIdleBaseInstance.DoPhysicsLogic();
    }
    
    public override void AnimationTriggerEvent(Enemy.AnimationTriggerType triggerType)
    {
        base.AnimationTriggerEvent(triggerType);

        enemy.EnemyIdleBaseInstance.DoAnimationTriggerEventLogic(triggerType);
    }
}
