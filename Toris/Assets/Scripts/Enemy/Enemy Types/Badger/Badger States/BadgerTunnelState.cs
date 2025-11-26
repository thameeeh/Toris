using UnityEngine;

public class BadgerTunnelState : EnemyState<Badger>
{
    public BadgerTunnelState(Badger enemy, EnemyStateMachine enemyStateMachine)
        : base(enemy, enemyStateMachine) { }

    public override void EnterState()
    {
        base.EnterState();

        enemy.BadgerTunnelBaseInstance.DoEnterLogic();
    }

    public override void ExitState()
    {
        base.ExitState();

        enemy.BadgerTunnelBaseInstance.DoExitLogic();
    }

    public override void FrameUpdate()
    {
        base.FrameUpdate();

        if (enemy.isRetreating)
        {
            if (enemy.BadgerTunnelBaseInstance.DistanceFromTargetPlayerPosition <= .25f)
            {
                Vector2 escapeDirection = ((Vector2)enemy.transform.position - (Vector2)enemy.PlayerTransform.position).normalized;
                if (escapeDirection != Vector2.zero)
                {
                    enemy.TunnelLineTarget = (Vector2)enemy.transform.position + escapeDirection * enemy.RunAwayDistance;
                }
            }

            if (!enemy.IsVisibleOnScreen())
            {
                enemy.DestroyBadger();
            }
        }
        else if (enemy.IsWithinStrikingDistance)
        {
            enemy.StateMachine.ChangeState(enemy.UnburrowState);
        }
        else if (enemy.BadgerTunnelBaseInstance.DistanceFromTargetPlayerPosition <= .1f)
        {
            enemy.StateMachine.ChangeState(enemy.UnburrowState);
        }

        enemy.BadgerTunnelBaseInstance.DoFrameUpdateLogic();
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        enemy.BadgerTunnelBaseInstance.DoPhysicsLogic();
    }

    public override void AnimationTriggerEvent(Enemy.AnimationTriggerType triggerType)
    {
        base.AnimationTriggerEvent(triggerType);

        enemy.BadgerTunnelBaseInstance.DoAnimationTriggerEventLogic(triggerType);
    }
}