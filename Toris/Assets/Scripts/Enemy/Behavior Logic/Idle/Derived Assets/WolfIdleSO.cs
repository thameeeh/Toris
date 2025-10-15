using UnityEngine;

[CreateAssetMenu(fileName = "Wolf_Idle_Wander", menuName = "Enemy Logic/Idle Logic/Wolf Idle Wander")]
public class WolfIdleSO : IdleSOBase<Wolf>
{
    [SerializeField] private float WanderRadius = 20f;
    [SerializeField] private float WanderTimer = 2f;
    [SerializeField] private float MovementSpeed = 3f;

    private float _timer;
    private Vector3 _wanderPoint;
    private Vector2 _moveDirection;

    public override void Initialize(GameObject gameObject, Wolf enemy, Transform player) 
    {
        base.Initialize(gameObject, enemy, player);

        _timer = WanderTimer;
        _wanderPoint = GetRandomWanderPoint();
    }

    public override void DoEnterLogic()
    {
        base.DoEnterLogic();
        enemy.animator.Play("Idle");
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
            _moveDirection = (_wanderPoint - enemy.transform.position).normalized;
        }

        
        

        if ((_wanderPoint - enemy.transform.position).sqrMagnitude < 0.01)
        {
            enemy.animator.SetBool("IsMoving", false);
            enemy.MoveEnemy(Vector2.zero);
        }
        else
        {
            enemy.animator.SetBool("IsMoving", true);
            enemy.MoveEnemy(_moveDirection * MovementSpeed);
            enemy.animator.SetFloat("DirectionX", _moveDirection.x);
            enemy.animator.SetFloat("DirectionY", _moveDirection.y);
        }
    }

    public override void DoPhysicsLogic()
    {
        base.DoPhysicsLogic();
    }

    
    public override void ResetValues()
    {
        base.ResetValues();
    }
    private Vector2 GetRandomWanderPoint()
    {
        return enemy.transform.position + Random.onUnitSphere * WanderRadius;
    }

    public override void DoAnimationTriggerEventLogic(Wolf.AnimationTriggerType triggerType)
    {
        base.DoAnimationTriggerEventLogic(triggerType);
    }
}
