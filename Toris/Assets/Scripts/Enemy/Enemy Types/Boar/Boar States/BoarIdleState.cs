public class BoarIdleState : EnemyState<Boar>
{
    public BoarIdleState(Boar enemy, EnemyStateMachine enemyStateMachine)
        : base(enemy, enemyStateMachine) { }

    public override void EnterState()
    {
        base.EnterState();
        enemy.BoarIdleBaseInstance?.DoEnterLogic();
    }

    public override void ExitState()
    {
        base.ExitState();
        enemy.BoarIdleBaseInstance?.DoExitLogic();
    }

    public override void FrameUpdate()
    {
        base.FrameUpdate();

        if (enemy.CanStartleCharge)
        {
            enemyStateMachine.ChangeState(enemy.ChargeState);
            return;
        }

        enemy.BoarIdleBaseInstance?.DoFrameUpdateLogic();
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        enemy.BoarIdleBaseInstance?.DoPhysicsLogic();
    }

    public override void AnimationTriggerEvent(Enemy.AnimationTriggerType triggerType)
    {
        base.AnimationTriggerEvent(triggerType);
        enemy.BoarIdleBaseInstance?.DoAnimationTriggerEventLogic(triggerType);
    }
}
