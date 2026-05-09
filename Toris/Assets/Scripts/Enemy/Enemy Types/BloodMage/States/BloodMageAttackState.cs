using UnityEngine;

public class BloodMageAttackState : EnemyState<BloodMage>
{
    public BloodMageAttackState(BloodMage enemy, EnemyStateMachine enemyStateMachine)
        : base(enemy, enemyStateMachine) { }

    public override void EnterState()
    {
        base.EnterState();
        enemy.MoveEnemy(Vector2.zero);
        enemy.BloodMageAttackBaseInstance?.DoEnterLogic();
#if UNITY_EDITOR
        enemy.DebugAttackLog("BloodMageAttackState enter -> requesting attack animation.");
#endif
        enemy.RequestAttackAnimation();
    }

    public override void ExitState()
    {
        base.ExitState();
        enemy.BloodMageAttackBaseInstance?.DoExitLogic();
#if UNITY_EDITOR
        enemy.DebugAttackLog("BloodMageAttackState exit.");
#endif
    }

    public override void FrameUpdate()
    {
        base.FrameUpdate();

        if (!enemy.HasCombatContext)
        {
#if UNITY_EDITOR
            enemy.DebugAttackLog("BloodMageAttackState -> IdleState because combat context is missing.");
#endif
            enemyStateMachine.ChangeState(enemy.IdleState);
            return;
        }

        enemy.BloodMageAttackBaseInstance?.DoFrameUpdateLogic();

        if (enemy.BloodMageAttackBaseInstance != null && enemy.BloodMageAttackBaseInstance.IsComplete)
        {
#if UNITY_EDITOR
            enemy.DebugAttackLog("BloodMageAttackState -> ChaseState because attack completed.");
#endif
            enemyStateMachine.ChangeState(enemy.ChaseState);
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        enemy.BloodMageAttackBaseInstance?.DoPhysicsLogic();
    }

    public override void AnimationTriggerEvent(Enemy.AnimationTriggerType triggerType)
    {
        base.AnimationTriggerEvent(triggerType);
        enemy.BloodMageAttackBaseInstance?.DoAnimationTriggerEventLogic(triggerType);
    }
}
