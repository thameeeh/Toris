public class NecromancerIdleState : EnemyState<Necromancer>
{
    public NecromancerIdleState(Necromancer enemy, EnemyStateMachine enemyStateMachine)
        : base(enemy, enemyStateMachine) { }

    public override void EnterState()
    {
        base.EnterState();
#if UNITY_EDITOR
        enemy.DebugAnimationLog("Gameplay state enter -> IdleState.");
#endif
        enemy.NecromancerIdleBaseInstance?.DoEnterLogic();
    }

    public override void ExitState()
    {
        base.ExitState();
#if UNITY_EDITOR
        enemy.DebugAnimationLog("Gameplay state exit -> IdleState.");
#endif
        enemy.NecromancerIdleBaseInstance?.DoExitLogic();
    }

    public override void FrameUpdate()
    {
        base.FrameUpdate();

        if (enemy.IsAggroed)
        {
            enemy.ResolveAggroTransition();
#if UNITY_EDITOR
            enemy.DebugAnimationLog("IdleState -> ChaseState because IsAggroed=true.");
#endif
            enemyStateMachine.ChangeState(enemy.ChaseState);
            return;
        }

        enemy.NecromancerIdleBaseInstance?.DoFrameUpdateLogic();
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        enemy.NecromancerIdleBaseInstance?.DoPhysicsLogic();
    }

    public override void AnimationTriggerEvent(Enemy.AnimationTriggerType triggerType)
    {
        base.AnimationTriggerEvent(triggerType);
        enemy.NecromancerIdleBaseInstance?.DoAnimationTriggerEventLogic(triggerType);
    }
}
