public class BloodMageDeadState : EnemyState<BloodMage>
{
    public BloodMageDeadState(BloodMage enemy, EnemyStateMachine enemyStateMachine)
        : base(enemy, enemyStateMachine) { }

    public override void EnterState()
    {
        base.EnterState();
        enemy.BloodMageDeadBaseInstance?.DoEnterLogic();
    }

    public override void ExitState()
    {
        base.ExitState();
        enemy.BloodMageDeadBaseInstance?.DoExitLogic();
    }

    public override void FrameUpdate()
    {
        base.FrameUpdate();
        enemy.BloodMageDeadBaseInstance?.DoFrameUpdateLogic();
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        enemy.BloodMageDeadBaseInstance?.DoPhysicsLogic();
    }

    public override void AnimationTriggerEvent(Enemy.AnimationTriggerType triggerType)
    {
        base.AnimationTriggerEvent(triggerType);
        enemy.BloodMageDeadBaseInstance?.DoAnimationTriggerEventLogic(triggerType);
    }
}
