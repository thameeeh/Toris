using UnityEngine;

public class BadgerWalkState : EnemyState<Badger>
{
    public BadgerWalkState(Badger enemy, EnemyStateMachine enemyStateMachine) 
        : base(enemy, enemyStateMachine) { }

    public override void EnterState()
    {
        base.EnterState();

        enemy.BadgerWalkBaseInstance.DoEnterLogic();
    }

    public override void ExitState()
    {
        base.ExitState();

        enemy.BadgerWalkBaseInstance.DoExitLogic();
    }

    public override void FrameUpdate()
    {
        base.FrameUpdate();

        if(!enemy.IsWondering) enemy.StateMachine.ChangeState(enemy.IdleState);

        enemy.BadgerWalkBaseInstance.DoFrameUpdateLogic();
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        enemy.BadgerWalkBaseInstance.DoPhysicsLogic();
    }
    public override void AnimationTriggerEvent(Enemy.AnimationTriggerType triggerType)
    {
        base.AnimationTriggerEvent(triggerType);

        enemy.BadgerWalkBaseInstance.DoAnimationTriggerEventLogic(triggerType);
    }
}
