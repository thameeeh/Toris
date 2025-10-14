using UnityEngine;

// Interface for enemy states in the state machine
// Since EnemyState is a generic class, we cannot use in the StateMachine directly
public interface IEnemyState
{
    void EnterState();
    void ExitState();
    void FrameUpdate();
    void PhysicsUpdate();
    void AnimationTriggerEvent(Enemy.AnimationTriggerType triggerType);
}
