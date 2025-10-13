using UnityEngine;

public class WolfAttackState : EnemyState<Wolf>
{
    public WolfAttackState(Wolf enemy, EnemyStateMachine enemyStateMachine) 
        : base(enemy, enemyStateMachine) { }

    public override void EnterState()
    {
        base.EnterState();

        enemy.EnemyAttackBaseInstance.DoEnterLogic();
    }

    public override void ExitState()
    {
        base.ExitState();

        enemy.EnemyAttackBaseInstance.DoExitLogic();
    }

    public override void FrameUpdate()
    {
        base.FrameUpdate();

        enemy.EnemyAttackBaseInstance.DoFrameUpdateLogic();
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        enemy.EnemyAttackBaseInstance.DoPhysicsLogic();
    }

    public override void AnimationTriggerEvent(Enemy.AnimationTriggerType triggerType)
    {
        base.AnimationTriggerEvent(triggerType);

        enemy.EnemyAttackBaseInstance.DoAnimationTriggerEventLogic(triggerType);
    }
}
