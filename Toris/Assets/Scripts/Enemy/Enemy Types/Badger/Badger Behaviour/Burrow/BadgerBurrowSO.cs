using UnityEngine;

[CreateAssetMenu(fileName = "Badger_Burrow", menuName = "Enemy Logic/Burrow Logic/Badger Burrow")]
public class BadgerBurrowSO : BurrowSO<Badger>
{
    private Vector2 _burrowDirection;

    public override void Initialize(GameObject gameObject, Badger enemy, Transform player)
    {
        base.Initialize(gameObject, enemy, player);
    }

    public override void DoEnterLogic()
    {
        base.DoEnterLogic();
        enemy.isBurrowed = true;
        enemy.isTunneling = false;

        enemy.isRetreating = enemy.ShouldRunAwayOnNextBurrow;
        enemy.ShouldRunAwayOnNextBurrow = false;

        if (enemy.isRetreating)
        {
            Vector2 awayDirection = ((Vector2)enemy.transform.position - (Vector2)enemy.PlayerTransform.position).normalized;
            if (awayDirection == Vector2.zero)
            {
                awayDirection = Random.insideUnitCircle.normalized;
            }
            enemy.RunAwayTargetPosition = (Vector2)enemy.transform.position + awayDirection * enemy.RunAwayDistance;
        }
        enemy.TargetPlayerPosition = enemy.PlayerTransform.position;
        enemy.TunnelLineTarget = enemy.isRetreating ? enemy.RunAwayTargetPosition : enemy.TargetPlayerPosition;
        enemy.animator.Play("Burrow BT");

        _burrowDirection = (enemy.TunnelLineTarget - (Vector2)enemy.transform.position).normalized;
        enemy.MoveEnemy(_burrowDirection);
        enemy.MoveEnemy(Vector2.zero);

        if (enemy.isRetreating)
        {
            enemy.isTunneling = true;
        }
    }

    public override void DoExitLogic()
    {
        base.DoExitLogic();
    }

    public override void DoFrameUpdateLogic()
    {
        base.DoFrameUpdateLogic();
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
}