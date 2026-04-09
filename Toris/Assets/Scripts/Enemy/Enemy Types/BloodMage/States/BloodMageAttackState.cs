using UnityEngine;

public class BloodMageAttackState : EnemyState<BloodMage>
{
    public BloodMageAttackState(BloodMage enemy, EnemyStateMachine enemyStateMachine)
        : base(enemy, enemyStateMachine) { }

    public override void EnterState()
    {
        base.EnterState();
        enemy.MoveEnemy(Vector2.zero);
        enemy.BloodMageAttackBaseInstance?.DoEnterLogic();
        enemy.RequestAttackAnimation();
    }

    public override void ExitState()
    {
        base.ExitState();
        enemy.BloodMageAttackBaseInstance?.DoExitLogic();
    }

    public override void FrameUpdate()
    {
        base.FrameUpdate();

        if (!enemy.HasCombatContext)
        {
            enemyStateMachine.ChangeState(enemy.IdleState);
            return;
        }

        enemy.BloodMageAttackBaseInstance?.DoFrameUpdateLogic();

        if (enemy.BloodMageAttackBaseInstance != null && enemy.BloodMageAttackBaseInstance.IsComplete)
            enemyStateMachine.ChangeState(enemy.ChaseState);
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        enemy.BloodMageAttackBaseInstance?.DoPhysicsLogic();
    }

    public override void AnimationTriggerEvent(Enemy.AnimationTriggerType triggerType)
    {
        base.AnimationTriggerEvent(triggerType);
        enemy.BloodMageAttackBaseInstance?.DoAnimationTriggerEventLogic(triggerType);
    }
}
