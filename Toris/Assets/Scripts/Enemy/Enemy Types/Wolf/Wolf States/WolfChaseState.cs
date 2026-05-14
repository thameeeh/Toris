using UnityEngine;

public class WolfChaseState : EnemyState<Wolf>
{
    public WolfChaseState(Wolf enemy, EnemyStateMachine enemyStateMachine) 
        : base(enemy, enemyStateMachine) { }

    public override void EnterState()
    {
        base.EnterState();

        enemy.EnemyChaseBaseInstance.DoEnterLogic();
        enemy.BeginChaseCommitment();

        if (enemy.CanHowl && enemy.pack != null && enemy.pack.CanLeaderHowl(enemy))
        {
            enemy.StateMachine.ChangeState(enemy.HowlState);
            return;
        }
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

        if (!enemy.ShouldRemainInChase())
        {
            if (enemy.HasHome)
                enemyStateMachine.ChangeState(enemy.ReturnHomeState);
            else
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
