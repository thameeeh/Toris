using UnityEngine;

public class HowlState : EnemyState<Wolf>
{
    private float _howlStartTime;
    private WolfHowl _wolfHowl;
    public HowlState(Wolf enemy, EnemyStateMachine enemyStateMachine) : base(enemy, enemyStateMachine)
    {
        _wolfHowl = enemy.EnemyHowlBaseInstance as WolfHowl;
    }

    public override void EnterState()
    {
        base.EnterState();
        _howlStartTime = Time.time;
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

        //check if howl duration is over and switch to chase state
        if (Time.time - _howlStartTime > _wolfHowl.HowlDuration)
        { 
            enemyStateMachine.ChangeState(enemy.ChaseState);
        }
        enemy.EnemyHowlBaseInstance.DoFrameUpdateLogic();
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
