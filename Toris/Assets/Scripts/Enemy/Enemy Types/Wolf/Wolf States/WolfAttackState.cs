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
        enemy.EnemyAttackBaseInstance.DoFrameUpdateLogic();

        if (!enemy.IsAggroed)
        {
            if (enemy.HasHome)
                enemyStateMachine.ChangeState(enemy.ReturnHomeState);
            else
                enemyStateMachine.ChangeState(enemy.IdleState);

            return;
        }

        if (!enemy.IsWithinStrikingDistance)
        {
            enemyStateMachine.ChangeState(enemy.ChaseState);
            return;
        }

        if (enemy.EnemyAttackBaseInstance.isComplete)
        {
            enemyStateMachine.ChangeState(enemy.ChaseState);
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
