public class BloodMageChaseState : EnemyState<BloodMage>
{
    public BloodMageChaseState(BloodMage enemy, EnemyStateMachine enemyStateMachine)
        : base(enemy, enemyStateMachine) { }

    public override void EnterState()
    {
        base.EnterState();
        enemy.BloodMageChaseBaseInstance?.DoEnterLogic();
    }

    public override void ExitState()
    {
        base.ExitState();
        enemy.BloodMageChaseBaseInstance?.DoExitLogic();
    }

    public override void FrameUpdate()
    {
        base.FrameUpdate();

        if (!enemy.HasCombatContext)
        {
            enemyStateMachine.ChangeState(enemy.IdleState);
            return;
        }

        enemy.BloodMageChaseBaseInstance?.DoFrameUpdateLogic();

        if (enemy.BloodMageChaseBaseInstance != null && enemy.BloodMageChaseBaseInstance.CanStartAttack)
            enemyStateMachine.ChangeState(enemy.AttackState);
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        enemy.BloodMageChaseBaseInstance?.DoPhysicsLogic();
    }

    public override void AnimationTriggerEvent(Enemy.AnimationTriggerType triggerType)
    {
        base.AnimationTriggerEvent(triggerType);
        enemy.BloodMageChaseBaseInstance?.DoAnimationTriggerEventLogic(triggerType);
    }
}
