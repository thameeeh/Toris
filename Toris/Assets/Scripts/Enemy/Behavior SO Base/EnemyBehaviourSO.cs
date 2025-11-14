using UnityEngine;

public class EnemyBehaviourSO<T> : ScriptableObject where T : Enemy
{
    protected T enemy;
    protected GameObject gameObject;
    protected Transform playerTransform;

    //Enemy Object, Enemy Script, "Player" Transform
    public virtual void Initialize(GameObject gameObject, T enemy, Transform player)
    {
        this.gameObject = gameObject;
        this.enemy = enemy;
        this.playerTransform = player;
    }
    public virtual void DoEnterLogic() { }
    public virtual void DoExitLogic()
    {
        ResetValues();
    }
    public virtual void DoFrameUpdateLogic() { }
    public virtual void DoPhysicsLogic() { }
    public virtual void DoAnimationTriggerEventLogic(Enemy.AnimationTriggerType triggerType) { }
    public virtual void ResetValues() { }
}
