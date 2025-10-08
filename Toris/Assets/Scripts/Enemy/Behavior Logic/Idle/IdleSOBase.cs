using UnityEngine;

public class EnemyIdleSOBase : ScriptableObject
{
    protected Enemy enemy;
    protected Transform transform;
    protected GameObject gameObject;
    protected Transform playerTransform;
    protected Animator animator;
    public virtual void Initialize(GameObject gameObject, Enemy enemy, Transform player)
    {
        this.gameObject = gameObject;
        transform = gameObject.transform;
        this.enemy = enemy;
        this.playerTransform = player;
        animator = gameObject.GetComponentInChildren<Animator>();
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

    }
    public virtual void DoPhysicsLogic()
    {

    }
    public virtual void DoAnimationTriggerEventLogic(Enemy.AnimationTriggerType triggerType)
    {
        animator.Play("Idle_SW");
    }
    public virtual void ResetValues()
    {

    }
}
