using UnityEngine;

[CreateAssetMenu(fileName = "Attack-Straight-Single Projectile", menuName = "Enemy Logic/Attack Logic/Double Test")]
public class EnemyDoubleTest : EnemyAttackSOBase
{
    [SerializeField] private Rigidbody2D BulletPrefab;

    [SerializeField] private float _bulletSpeed = 10f;
    [SerializeField] private float _timeBetweenShots = 2f;
    [SerializeField] private float _timeTillExit = 3f;

    private float _timer;
    private float _exitTimer;

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
            Vector2 sideBullet = new Vector2(-dir.y, dir.x) * 0.05f;

            // Left bullet
            Rigidbody2D bullet1 = Instantiate(BulletPrefab,
                enemy.transform.position + (Vector3)sideBullet,
                Quaternion.identity);
            bullet1.linearVelocity = dir * _bulletSpeed;

            // Right bullet
            Rigidbody2D bullet2 = Instantiate(BulletPrefab,
                enemy.transform.position - (Vector3)sideBullet,
                Quaternion.identity);
            bullet2.linearVelocity = dir * _bulletSpeed;

            Destroy(bullet1.gameObject, 5f);
            Destroy(bullet2.gameObject, 5f);
        }

        if (!enemy.IsWithinStrikingDistance)
        {
            _exitTimer += Time.deltaTime;
            if (_exitTimer > _timeTillExit)
                enemy.StateMachine.ChangeState(enemy.ChaseState);
        }

        else
        {
            _exitTimer -= 0f;
        }

        _timer += Time.deltaTime;
    }

    public override void DoPhysicsLogic()
    {
        base.DoPhysicsLogic();
    }

    public override void Initialize(GameObject gameObject, Enemy enemy, Transform player)
    {
        base.Initialize(gameObject, enemy, player);
    }

    public override void ResetValues()
    {
        base.ResetValues();
    }
}
