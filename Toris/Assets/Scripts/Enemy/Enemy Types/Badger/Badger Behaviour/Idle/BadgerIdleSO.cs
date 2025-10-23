using UnityEngine;
using UnityEngine.EventSystems;

[CreateAssetMenu(fileName = "Badger_Idle", menuName = "Enemy Logic/Idle Logic/Badger Idle")]
public class BadgerIdleSO : IdleSOBase<Badger>
{
    [SerializeField] private float WanderRadius = 20f;
    [SerializeField] private float WanderTimer = 2f;
    private float MovementSpeed;

    private float _timer;
    private Vector3 _wanderPoint;
    private Vector2 _moveDirection;

    public override void Initialize(GameObject gameObject, Badger enemy, Transform player)
    {
        base.Initialize(gameObject, enemy, player);
    }

    public override void DoEnterLogic()
    {
        base.DoEnterLogic();

        enemy.IsCurrentlyWondering(true);
        MovementSpeed = enemy.WalkingSpeed;
    }

    public override void DoExitLogic()
    {
        base.DoExitLogic();

        enemy.IsCurrentlyWondering(false);
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



        if ((_wanderPoint - enemy.transform.position).sqrMagnitude < 0.1)
        {
            enemy.animator.SetBool("IsMoving", false);
            enemy.MoveEnemy(Vector2.zero);
        }
        else
        {
            enemy.animator.SetBool("IsMoving", true);
            enemy.MoveEnemy(_moveDirection * MovementSpeed);
            enemy.UpdateAnimationDirection(_moveDirection);
        }
    }

    public override void DoPhysicsLogic()
    {
        base.DoPhysicsLogic();
    }
    public override void DoAnimationTriggerEventLogic(Enemy.AnimationTriggerType triggerType)
    {
        base.DoAnimationTriggerEventLogic(triggerType);
    }

    public override void ResetValues()
    {
        base.ResetValues();
    }

    private Vector2 GetRandomWanderPoint()
    {
        return enemy.transform.position + Random.onUnitSphere * WanderRadius;
    }
}
