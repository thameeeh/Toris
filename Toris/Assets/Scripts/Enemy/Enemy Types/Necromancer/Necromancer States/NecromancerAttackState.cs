using UnityEngine;

public class NecromancerAttackState : EnemyState<Necromancer>
{
    public NecromancerAttackState(Necromancer enemy, EnemyStateMachine enemyStateMachine)
        : base(enemy, enemyStateMachine) { }

    public override void EnterState()
    {
        base.EnterState();

        enemy.MoveEnemy(Vector2.zero);
#if UNITY_EDITOR
        enemy.DebugAnimationLog($"Gameplay state enter -> AttackState. Requesting {enemy.PendingAttackType} animation.");
#endif
        enemy.NecromancerAttackBaseInstance?.DoEnterLogic();
        enemy.RequestAttackAnimation();
    }

    public override void ExitState()
    {
        base.ExitState();
#if UNITY_EDITOR
        enemy.DebugAnimationLog("Gameplay state exit -> AttackState.");
#endif
        enemy.NecromancerAttackBaseInstance?.DoExitLogic();
    }

    public override void FrameUpdate()
    {
        base.FrameUpdate();

        enemy.NecromancerAttackBaseInstance?.DoFrameUpdateLogic();

        if (!enemy.IsAggroed)
        {
#if UNITY_EDITOR
            enemy.DebugAnimationLog("AttackState -> IdleState because IsAggroed=false.");
#endif
            enemyStateMachine.ChangeState(enemy.IdleState);
            return;
        }

        if (enemy.NecromancerAttackBaseInstance != null && enemy.NecromancerAttackBaseInstance.IsComplete)
        {
#if UNITY_EDITOR
            enemy.DebugAnimationLog("AttackState -> ChaseState because attack animation finished.");
#endif
            enemyStateMachine.ChangeState(enemy.ChaseState);
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        enemy.NecromancerAttackBaseInstance?.DoPhysicsLogic();
    }

    public override void AnimationTriggerEvent(Enemy.AnimationTriggerType triggerType)
    {
        base.AnimationTriggerEvent(triggerType);
        enemy.NecromancerAttackBaseInstance?.DoAnimationTriggerEventLogic(triggerType);
    }
}
