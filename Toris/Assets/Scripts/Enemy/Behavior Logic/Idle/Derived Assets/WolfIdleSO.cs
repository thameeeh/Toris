using UnityEngine;

[CreateAssetMenu(fileName = "Wolf_Idle_Wander", menuName = "Enemy Logic/Idle Logic/Wolf Idle Wander")]
public class WolfIdleSO : IdleSOBase<Wolf>
{
    [SerializeField] private float WanderRadius = 5f;
    [SerializeField] private float WanderTimer = 2f;
    [SerializeField] private float MovementSpeed = 3f;

    private float _timer;
    private Vector2 _wanderPoint;

    public override void Initialize(GameObject gameObject, Wolf enemy, Transform player) 
    {
        base.Initialize(gameObject, enemy, player);

        _timer = WanderTimer;
        _wanderPoint = GetRandomWanderPoint();
    }

    public override void DoEnterLogic()
    {
        base.DoEnterLogic();
    }

    public override void DoExitLogic()
    {
        base.DoExitLogic();
    }

    public override void DoFrameUpdateLogic()
    {
        base.DoFrameUpdateLogic();

        _timer += Time.deltaTime;

        if (_timer >= WanderTimer)
        {
            _wanderPoint = GetRandomWanderPoint();
            _timer = 0;
        }

        Vector2 moveDirection = (_wanderPoint - (Vector2)enemy.transform.position).normalized;
        enemy.MoveEnemy(moveDirection * MovementSpeed);

        enemy.animator.SetFloat("DirectionX", moveDirection.x);
        enemy.animator.SetFloat("DirectionY", moveDirection.y);
    }

    public override void DoPhysicsLogic()
    {
        base.DoPhysicsLogic();
    }

    
    public override void ResetValues()
    {
        base.ResetValues();
    }
    private Vector3 GetRandomWanderPoint()
    {
        return (Vector2)enemy.transform.position + Random.insideUnitCircle * WanderRadius;
    }

    public override void DoAnimationTriggerEventLogic(Wolf.AnimationTriggerType triggerType)
    {
        base.DoAnimationTriggerEventLogic(triggerType);
    }
}
