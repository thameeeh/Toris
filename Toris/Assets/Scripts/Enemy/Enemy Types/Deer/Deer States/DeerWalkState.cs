public class DeerWalkState : EnemyState<Deer>
{
    public DeerWalkState(Deer enemy, EnemyStateMachine enemyStateMachine)
        : base(enemy, enemyStateMachine) { }

    public override void EnterState()
    {
        base.EnterState();
        enemy.EnemyWalkBaseInstance.DoEnterLogic();
    }

    public override void ExitState()
    {
        base.ExitState();
        enemy.EnemyWalkBaseInstance.DoExitLogic();
    }

    public override void FrameUpdate()
    {
        base.FrameUpdate();
        enemy.EnemyWalkBaseInstance.DoFrameUpdateLogic();
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        enemy.EnemyWalkBaseInstance.DoPhysicsLogic();
    }

    public override void AnimationTriggerEvent(Enemy.AnimationTriggerType triggerType)
    {
        base.AnimationTriggerEvent(triggerType);
        enemy.EnemyWalkBaseInstance.DoAnimationTriggerEventLogic(triggerType);
    }
}
