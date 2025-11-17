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

        enemy.animator.Play("Burrow BT");

        _burrowDirection = (enemy.TargetPlayerPosition - (Vector2)enemy.transform.position).normalized;
        enemy.MoveEnemy(_burrowDirection); //to play animation in right direction
        enemy.MoveEnemy(Vector2.zero);
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
