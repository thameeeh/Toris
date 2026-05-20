public class BoarWanderState : EnemyState<Boar>
{
    public BoarWanderState(Boar enemy, EnemyStateMachine enemyStateMachine)
        : base(enemy, enemyStateMachine) { }

    public override void EnterState()
    {
        base.EnterState();
        enemy.BoarWanderBaseInstance?.DoEnterLogic();
    }

    public override void ExitState()
    {
        base.ExitState();
        enemy.BoarWanderBaseInstance?.DoExitLogic();
    }

    public override void FrameUpdate()
    {
        base.FrameUpdate();

        if (enemy.CanStartleCharge)
        {
            enemyStateMachine.ChangeState(enemy.ChargeState);
            return;
        }

        enemy.BoarWanderBaseInstance?.DoFrameUpdateLogic();
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        enemy.BoarWanderBaseInstance?.DoPhysicsLogic();
    }

    public override void AnimationTriggerEvent(Enemy.AnimationTriggerType triggerType)
    {
        base.AnimationTriggerEvent(triggerType);
        enemy.BoarWanderBaseInstance?.DoAnimationTriggerEventLogic(triggerType);
    }
}
