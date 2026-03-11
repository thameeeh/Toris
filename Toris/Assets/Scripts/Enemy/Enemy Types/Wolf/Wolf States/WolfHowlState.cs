using UnityEngine;

public class WolfHowlState : EnemyState<Wolf>
{
    public WolfHowlState(Wolf enemy, EnemyStateMachine enemyStateMachine) 
        : base(enemy, enemyStateMachine) { }

    public override void EnterState()
    {
        base.EnterState();

        // decision
        bool canHowl =
            enemy.CanHowl &&
            enemy.pack != null &&
            enemy.pack.EnsureLeader(enemy) &&
            enemy.pack.CanLeaderHowl(enemy);

        if (!canHowl)
        {
            enemyStateMachine.ChangeState(enemy.ChaseState);
            return;
        }

        enemy.EnemyHowlBaseInstance.DoEnterLogic();
    }

    public override void ExitState()
    {
        base.ExitState();

        enemy.EnemyHowlBaseInstance.DoExitLogic();
    }

    public override void FrameUpdate()
    {
        base.FrameUpdate();

        enemy.EnemyHowlBaseInstance.DoFrameUpdateLogic();

        if (enemy.EnemyHowlBaseInstance.isComplete)
        {
            enemy.pack.HandleLeaderHowl(enemy);
            enemyStateMachine.ChangeState(enemy.ChaseState);
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        enemy.EnemyHowlBaseInstance.DoPhysicsLogic();
    }

    public override void AnimationTriggerEvent(Enemy.AnimationTriggerType triggerType)
    {
        base.AnimationTriggerEvent(triggerType);

        enemy.EnemyHowlBaseInstance.DoAnimationTriggerEventLogic(triggerType);
    }
}
