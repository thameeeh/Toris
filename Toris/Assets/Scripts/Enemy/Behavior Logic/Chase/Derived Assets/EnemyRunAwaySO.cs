using UnityEngine;
[CreateAssetMenu(fileName = "Generic_Chase_Runaway", menuName = "Enemy Logic/Chase Logic/Generic Chase Runaway")]
public class EnemyRunAway : ChaseSOBase<Generic>
{
    [SerializeField] private float _runawaySpeed = 0.2f;

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

        Vector2 moveDirection = (enemy.transform.position - playerTransform.position).normalized;
        enemy.MoveEnemy(moveDirection * _runawaySpeed);

        if (enemy.IsWithinStrikingDistance)
        {
            //enemy.StateMachine.ChangeState(enemy.AttackState);
        }

        if (!enemy.IsAggroed)
        {
            enemy.StateMachine.ChangeState(enemy.IdleState);
            return;
        }
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
    }
}
