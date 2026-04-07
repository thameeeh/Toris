public class NecromancerChaseState : EnemyState<Necromancer>
{
    public NecromancerChaseState(Necromancer enemy, EnemyStateMachine enemyStateMachine)
        : base(enemy, enemyStateMachine) { }

    public override void EnterState()
    {
        base.EnterState();
#if UNITY_EDITOR
        enemy.DebugAnimationLog("Gameplay state enter -> ChaseState.");
#endif
        enemy.NecromancerChaseBaseInstance?.DoEnterLogic();
    }

    public override void ExitState()
    {
        base.ExitState();
#if UNITY_EDITOR
        enemy.DebugAnimationLog("Gameplay state exit -> ChaseState.");
#endif
        enemy.NecromancerChaseBaseInstance?.DoExitLogic();
    }

    public override void FrameUpdate()
    {
        base.FrameUpdate();

        if (!enemy.IsAggroed)
        {
#if UNITY_EDITOR
            enemy.DebugAnimationLog("ChaseState -> IdleState because IsAggroed=false.");
#endif
            enemyStateMachine.ChangeState(enemy.IdleState);
            return;
        }

        enemy.NecromancerChaseBaseInstance?.DoFrameUpdateLogic();

        NecromancerChaseSO chaseLogic = enemy.NecromancerChaseBaseInstance;

        if (chaseLogic != null
            && chaseLogic.CanStartSelectedAttack)
        {
            enemy.SetPendingAttackType(chaseLogic.SelectedAttackType);
#if UNITY_EDITOR
            enemy.DebugAnimationLog($"ChaseState -> AttackState with {chaseLogic.SelectedAttackType} because range, cooldown, and floater form are ready.");
#endif
            enemyStateMachine.ChangeState(enemy.AttackState);
            return;
        }

#if UNITY_EDITOR
        if (chaseLogic != null
            && (chaseLogic.IsInCastingRange || chaseLogic.IsInPanicRange)
            && enemy.IsReadyToCastAnimation
            && !enemy.CanStartAttack(chaseLogic.SelectedAttackType))
        {
            enemy.DebugAnimationDecision($"Attack gated in ChaseState: {chaseLogic.SelectedAttackType} cooldown is not ready.");
        }

        if (chaseLogic != null
            && (chaseLogic.IsInCastingRange || chaseLogic.IsInPanicRange)
            && enemy.CanStartAnyAttack
            && !enemy.IsReadyToCastAnimation)
        {
            enemy.DebugAnimationDecision("Attack gated in ChaseState: waiting for floater attack form readiness.");
        }
#endif
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        enemy.NecromancerChaseBaseInstance?.DoPhysicsLogic();
    }

    public override void AnimationTriggerEvent(Enemy.AnimationTriggerType triggerType)
    {
        base.AnimationTriggerEvent(triggerType);
        enemy.NecromancerChaseBaseInstance?.DoAnimationTriggerEventLogic(triggerType);
    }
}
