using UnityEngine;

public class GenericAttackState : EnemyState<Generic>
{
    public GenericAttackState(Generic enemy, EnemyStateMachine enemyStateMachine) 
        : base(enemy, enemyStateMachine) { }

    public override void EnterState()
    {
        base.EnterState();

#if UNITY_EDITOR
        enemy.DebugAttackLog("GenericAttackState enter.");
#endif
        enemy.EnemyAttackBaseInstance.DoEnterLogic();
    }

    public override void ExitState()
    {
        base.ExitState();

        enemy.EnemyAttackBaseInstance.DoExitLogic();
#if UNITY_EDITOR
        enemy.DebugAttackLog("GenericAttackState exit.");
#endif
    }

    public override void FrameUpdate()
    {
        base.FrameUpdate();

        enemy.EnemyAttackBaseInstance.DoFrameUpdateLogic();
    }
    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        enemy.EnemyAttackBaseInstance.DoPhysicsLogic();
    }
    public override void AnimationTriggerEvent(Enemy.AnimationTriggerType triggerType)
    {
        base.AnimationTriggerEvent(triggerType);

        enemy.EnemyAttackBaseInstance.DoAnimationTriggerEventLogic(triggerType);
    }
}
