public class NecromancerDeadState : EnemyState<Necromancer>
{
    public NecromancerDeadState(Necromancer enemy, EnemyStateMachine enemyStateMachine)
        : base(enemy, enemyStateMachine) { }

    public override void EnterState()
    {
        base.EnterState();
#if UNITY_EDITOR
        enemy.DebugAnimationLog("Gameplay state enter -> DeadState.");
#endif
        enemy.NecromancerDeadBaseInstance?.DoEnterLogic();
    }

    public override void ExitState()
    {
        base.ExitState();
#if UNITY_EDITOR
        enemy.DebugAnimationLog("Gameplay state exit -> DeadState.");
#endif
        enemy.NecromancerDeadBaseInstance?.DoExitLogic();
    }

    public override void FrameUpdate()
    {
        base.FrameUpdate();
        enemy.NecromancerDeadBaseInstance?.DoFrameUpdateLogic();
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        enemy.NecromancerDeadBaseInstance?.DoPhysicsLogic();
    }

    public override void AnimationTriggerEvent(Enemy.AnimationTriggerType triggerType)
    {
        base.AnimationTriggerEvent(triggerType);
        enemy.NecromancerDeadBaseInstance?.DoAnimationTriggerEventLogic(triggerType);
    }
}
