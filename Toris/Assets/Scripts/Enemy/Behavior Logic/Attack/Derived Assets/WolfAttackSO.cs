using UnityEngine;

[CreateAssetMenu(fileName = "Wolf_Attack_QuickBite", menuName = "Enemy Logic/Attack Logic/Wolf Attack QuickBite")]
public class WolfAttackSO : AttackSOBase<Wolf>
{
    private Vector2 _animationDirection = Vector2.zero;
    
    public override void Initialize(GameObject gameObject, Wolf enemy, Transform player)
    {
        base.Initialize(gameObject, enemy, player);
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

        Vector2 moveDirection = (playerTransform.position - enemy.transform.position).normalized * enemy.MovementSpeed;;

        if (enemy.IsMovingWhileBiting)
        {
            enemy.MoveEnemy(moveDirection);
        }
        else enemy.MoveEnemy(Vector2.zero);
    }

    public override void DoPhysicsLogic()
    {
        base.DoPhysicsLogic();

        _animationDirection = enemy.PlayerTransform.position - enemy.transform.position;
        enemy.UpdateAnimationDirection(_animationDirection.normalized);
        enemy.animator.SetTrigger("Attack");
    }

    public override void ResetValues()
    {
        base.ResetValues();

        Debug.Log("Values have been reset");
        enemy.animator.ResetTrigger("Attack");
    }
    
    public override void DoAnimationTriggerEventLogic(Wolf.AnimationTriggerType triggerType)
    {
        base.DoAnimationTriggerEventLogic(triggerType);
    }
}
