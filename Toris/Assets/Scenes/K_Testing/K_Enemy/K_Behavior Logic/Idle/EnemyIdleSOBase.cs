using UnityEngine;

public class EnemyIdleSOBase : ScriptableObject
{
    protected Enemy enemy;
    protected Transform transform;
    protected GameObject gameObject;
    protected Transform playerTransform;

    public virtual void Initialize(GameObject gameObject, Enemy enemy, Transform player)
    {
        this.gameObject = gameObject;
        transform = gameObject.transform;
        this.enemy = enemy;
        this.playerTransform = player;
    }

    public virtual void DoEnterLogic()
    {

    }
    public virtual void DoExitLogic()
    {
        ResetValues();
    }
    public virtual void DoFrameUpdateLogic()
    {
        if (enemy.IsAggroed)
        {
            enemy.StateMachine.ChangeState(enemy.ChaseState);
        }
    }
    public virtual void DoPhysicsLogic()
    {

    }
    public virtual void DoAnimationTriggerEventLogic(Enemy.AnimationTriggerType triggerType)
    {

    }
    public virtual void ResetValues()
    {

    }
}
