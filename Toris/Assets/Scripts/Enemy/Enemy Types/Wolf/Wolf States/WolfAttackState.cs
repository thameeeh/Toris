using UnityEngine;

public class WolfAttackState : EnemyState<Wolf>
{
    public WolfAttackState(Wolf enemy, EnemyStateMachine enemyStateMachine) 
        : base(enemy, enemyStateMachine) { }

    public override void EnterState()
    {
        base.EnterState();

        enemy.MoveEnemy(Vector2.zero);

        enemy.EnemyAttackBaseInstance.DoEnterLogic();
        enemy.animator.SetTrigger("Attack");
    }

    public override void ExitState()
    {
        base.ExitState();

        enemy.EnemyAttackBaseInstance.DoExitLogic();
    }

    public override void FrameUpdate()
    {
        base.FrameUpdate();

        enemy.EnemyAttackBaseInstance.DoFrameUpdateLogic();

        if (enemy.EnemyAttackBaseInstance.isComplete)
        {
            enemyStateMachine.ChangeState(enemy.ChaseState);
            return;
        }

        if (!enemy.IsAggroed)
        {
            enemyStateMachine.ChangeState(enemy.IdleState);
            return;
        }
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
