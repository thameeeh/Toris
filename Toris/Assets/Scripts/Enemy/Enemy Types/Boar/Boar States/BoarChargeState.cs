public class BoarChargeState : EnemyState<Boar>
{
    public BoarChargeState(Boar enemy, EnemyStateMachine enemyStateMachine)
        : base(enemy, enemyStateMachine) { }

    public override void EnterState()
    {
        base.EnterState();
        enemy.BoarChargeBaseInstance?.DoEnterLogic();
    }

    public override void ExitState()
    {
        base.ExitState();
        enemy.BoarChargeBaseInstance?.DoExitLogic();
    }

    public override void FrameUpdate()
    {
        base.FrameUpdate();

        enemy.BoarChargeBaseInstance?.DoFrameUpdateLogic();
        if (enemy.BoarChargeBaseInstance == null || !enemy.BoarChargeBaseInstance.IsComplete)
            return;

        enemyStateMachine.ChangeState(enemy.FleeState);
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        enemy.BoarChargeBaseInstance?.DoPhysicsLogic();
    }

    public override void AnimationTriggerEvent(Enemy.AnimationTriggerType triggerType)
    {
        base.AnimationTriggerEvent(triggerType);
        enemy.BoarChargeBaseInstance?.DoAnimationTriggerEventLogic(triggerType);
    }
}
