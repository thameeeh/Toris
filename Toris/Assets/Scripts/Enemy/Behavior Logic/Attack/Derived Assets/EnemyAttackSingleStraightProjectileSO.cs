using UnityEngine;

[CreateAssetMenu(fileName = "Generic_Attack_SingleProjectile", menuName = "Enemy Logic/Attack Logic/Generic Attack SingleProjectile")]
public class EnemyAttackSingleStraightProjectile : AttackSOBase<Generic>
{
    [SerializeField] private Rigidbody2D BulletPrefab;

    [SerializeField] private float _bulletSpeed = 10f;
    [SerializeField] private float _timeBetweenShots = 2f;
    [SerializeField] private float _timeTillExit = 3f;

    private float _timer;
    private float _exitTimer;

    //when writing base.DoSomething(), it calls the method in the base class (EnemyAttackSOBase)
    //so basically it first does parent class logic, then child class logic
    public override void DoAnimationTriggerEventLogic(Enemy.AnimationTriggerType triggerType)
    {
        base.DoAnimationTriggerEventLogic(triggerType);
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

        enemy.MoveEnemy(Vector2.zero);

        if (_timer > _timeBetweenShots)
        {
            _timer = 0f;

            Vector2 dir = (playerTransform.position - enemy.transform.position).normalized;

            Rigidbody2D bullet = GameObject.Instantiate(BulletPrefab, enemy.transform.position, Quaternion.identity);
            bullet.linearVelocity = dir * _bulletSpeed;

            Destroy(bullet.gameObject, 3f);
        }

        if (!enemy.IsWithinStrikingDistance)
        {
            _exitTimer += Time.deltaTime;
            if (_exitTimer > _timeTillExit)
                enemy.StateMachine.ChangeState(enemy.ChaseState);
        }

        else
        {
            _exitTimer = 0f;
        }

        _timer += Time.deltaTime;
    }

    public override void DoPhysicsLogic()
    {
        base.DoPhysicsLogic();
    }

    public override void Initialize(GameObject gameObject, Generic enemy, Transform player)
    {
        base.Initialize(gameObject, enemy, player);
    }

    public override void ResetValues()
    {
        base.ResetValues();

        _timer = 0f;
        _exitTimer = 0f;
    }
}
