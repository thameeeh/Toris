public class BloodMageIdleState : EnemyState<BloodMage>
{
    public BloodMageIdleState(BloodMage enemy, EnemyStateMachine enemyStateMachine)
        : base(enemy, enemyStateMachine) { }

    public override void EnterState()
    {
        base.EnterState();
        enemy.BloodMageIdleBaseInstance?.DoEnterLogic();
    }

    public override void ExitState()
    {
        base.ExitState();
        enemy.BloodMageIdleBaseInstance?.DoExitLogic();
    }

    public override void FrameUpdate()
    {
        base.FrameUpdate();

        enemy.BloodMageIdleBaseInstance?.DoFrameUpdateLogic();

        if (enemy.BloodMageIdleBaseInstance != null && enemy.BloodMageIdleBaseInstance.IsReadyToLeaveIdle)
            enemyStateMachine.ChangeState(enemy.ChaseState);
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        enemy.BloodMageIdleBaseInstance?.DoPhysicsLogic();
    }

    public override void AnimationTriggerEvent(Enemy.AnimationTriggerType triggerType)
    {
        base.AnimationTriggerEvent(triggerType);
        enemy.BloodMageIdleBaseInstance?.DoAnimationTriggerEventLogic(triggerType);
    }
}
