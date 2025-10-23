using UnityEngine;

public class WolfChaseState : EnemyState<Wolf>
{
    public WolfChaseState(Wolf enemy, EnemyStateMachine enemyStateMachine) 
        : base(enemy, enemyStateMachine) { }

    public override void EnterState()
    {
        base.EnterState();

        enemy.EnemyChaseBaseInstance.DoEnterLogic();
        enemy.animator.Play("Run");
    }

    public override void ExitState()
    {
        base.ExitState();

        enemy.EnemyChaseBaseInstance.DoExitLogic();
    }

    public override void FrameUpdate()
    {
        base.FrameUpdate();

        enemy.EnemyChaseBaseInstance.DoFrameUpdateLogic();

        if (enemy.IsWithinStrikingDistance)
        {
            enemyStateMachine.ChangeState(enemy.AttackState);
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

        enemy.EnemyChaseBaseInstance.DoPhysicsLogic();
    }

    public override void AnimationTriggerEvent(Enemy.AnimationTriggerType triggerType)
    {
        base.AnimationTriggerEvent(triggerType);

        enemy.EnemyChaseBaseInstance.DoAnimationTriggerEventLogic(triggerType);
    }
}
