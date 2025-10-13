using UnityEngine;

// Base class for enemy states, using generics to specify the type of enemy
// As an example, EnemyState<Wolf> for a Wolf enemy
// Since Enemy Types inherit from Enemy
public class EnemyState<T> : IEnemyState where T : Enemy
{
    protected T enemy;
    protected EnemyStateMachine enemyStateMachine;

    public EnemyState(T enemy, EnemyStateMachine enemyStateMachine)
    {
        this.enemy = enemy;
        this.enemyStateMachine = enemyStateMachine;
    }

    public virtual void EnterState() { }
    public virtual void ExitState() { }
    public virtual void FrameUpdate() { }
    public virtual void PhysicsUpdate() { }
    public virtual void AnimationTriggerEvent(Enemy.AnimationTriggerType triggerType) { }
}
