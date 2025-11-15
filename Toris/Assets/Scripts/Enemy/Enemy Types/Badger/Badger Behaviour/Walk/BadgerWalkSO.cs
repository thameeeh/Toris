using UnityEngine;

[CreateAssetMenu(fileName = "Badger_Walk", menuName = "Enemy Logic/Walk Logic/Badger Walk")]
public class BadgerWalkSO : WalkSOBase<Badger>
{
    [SerializeField] private float WanderRadius = 5f;
    [SerializeField] private float WanderTimer = 2f;

    private float _distance;
    private float _timer;

    Vector3 wanderPoint;
    Vector3 currentDirection;
    public override void Initialize(GameObject gameObject, Badger enemy, Transform player)
    {
        base.Initialize(gameObject, enemy, player);
    }

    public override void DoEnterLogic()
    {
        base.DoEnterLogic();

        enemy.animator.SetBool("IsMoving", true);
        wanderPoint = GetRandomWanderPoint();
    }

    public override void DoExitLogic()
    {
        base.DoExitLogic();
        enemy.animator.SetBool("IsMoving", false);
    }

    public override void DoFrameUpdateLogic()
    {
        base.DoFrameUpdateLogic();
    }

    public override void DoPhysicsLogic()
    {
        base.DoPhysicsLogic();

        _distance = Vector2.Distance(enemy.transform.position, wanderPoint);

        if (_distance > .1f)
        {
            currentDirection = (wanderPoint - enemy.transform.position).normalized;
            enemy.MoveEnemy(enemy.WalkSpeed * currentDirection);
        }

        _timer += Time.fixedDeltaTime;

        if (_distance <= .1f || _timer >= WanderTimer)
        {
            enemy.IsWondering = false;
        }

    }
    public override void DoAnimationTriggerEventLogic(Enemy.AnimationTriggerType triggerType)
    {
        base.DoAnimationTriggerEventLogic(triggerType);
    }

    public override void ResetValues()
    {
        base.ResetValues();

        _timer = 0f;
    }
    private Vector2 GetRandomWanderPoint()
    {
        return enemy.transform.position + Random.onUnitSphere * WanderRadius;
    }
}
