using UnityEngine;

[CreateAssetMenu(fileName = "Badger_Burrow", menuName = "Enemy Logic/Burrow Logic/Badger Burrow")]
public class BadgerBurrowSO : BurrowSO<Badger>
{
    private float _burrowSpeed;
    private Vector2 _burrowDirection;

    public override void Initialize(GameObject gameObject, Badger enemy, Transform player)
    {
        base.Initialize(gameObject, enemy, player);
    }

    public override void DoEnterLogic()
    {
        base.DoEnterLogic();

        enemy.animator.Play("Burrow");

        enemy.IsCurrentlyBurrowing(true);
        _burrowSpeed = enemy.BurrowSpeed;
        _burrowDirection = (enemy.TargetPlayerPosition - (Vector2)enemy.transform.position).normalized;
    }

    public override void DoExitLogic()
    {
        base.DoExitLogic();
    }

    public override void DoFrameUpdateLogic()
    {
        base.DoFrameUpdateLogic();

        float distanceToTarget = Vector2.Distance(enemy.transform.position, enemy.TargetPlayerPosition);
        if (distanceToTarget > 0.1f)
        {
            enemy.MoveEnemy(_burrowDirection * enemy.BurrowSpeed);
        }
        else
        {
            enemy.MoveEnemy(Vector2.zero);

            if(!enemy.IsWondering) enemy.StateMachine.ChangeState(enemy.AttackState);
        }

        if(enemy.IsBurrowing) {
            enemy.MoveEnemy(Vector2.zero);
        }
        if (!enemy.IsBurrowing)
        {
            enemy.animator.SetBool("IsTunneling", true);
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
}
