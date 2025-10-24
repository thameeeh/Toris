using UnityEngine;

public class BadgerIdleState : EnemyState<Badger>
{
    public float IdleWanderTimer;

    private float _timer;
    public BadgerIdleState(Badger enemy, EnemyStateMachine enemyStateMachine) 
        : base(enemy, enemyStateMachine) { }

    public override void EnterState()
    {
        base.EnterState();
        enemy.IsWondering = true;
        enemy.BadgerIdleBaseInstance.DoEnterLogic();
    }

    public override void ExitState()
    {
        base.ExitState();

        _timer = 0;

        enemy.BadgerIdleBaseInstance.DoExitLogic();
    }

    public override void FrameUpdate()
    {
        base.FrameUpdate();
        
        _timer += Time.deltaTime;

        if (_timer >= IdleWanderTimer)
        {
            enemy.IsWondering = false;
            if (enemy.IsAggroed)
            {
                enemy.TargetPlayerPosition = enemy.PlayerTransform.position;
                enemyStateMachine.ChangeState(enemy.BurrowState);
            }
            
        }

        enemy.BadgerIdleBaseInstance.DoFrameUpdateLogic();
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        enemy.BadgerIdleBaseInstance.DoPhysicsLogic();
    }

    public override void AnimationTriggerEvent(Enemy.AnimationTriggerType triggerType)
    {
        base.AnimationTriggerEvent(triggerType);

        enemy.BadgerIdleBaseInstance.DoAnimationTriggerEventLogic(triggerType);
    }
}
