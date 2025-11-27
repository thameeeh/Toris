using UnityEngine;

[CreateAssetMenu(fileName = "Badger_Unburrow", menuName = "Enemy Logic/Attack Logic/Badger Unburrow/Attack")]
public class BadgerUnburrowSO : TunnelSOBase<Badger>
{

    public override void Initialize(GameObject gameObject, Badger enemy, Transform player)
    {
        base.Initialize(gameObject, enemy, player);
    }

    public override void DoEnterLogic()
    {
        base.DoEnterLogic();

        enemy.animator.Play("Unburrow BT");
        enemy.MoveEnemy(Vector2.zero);
        enemy.isTunneling = false;
        enemy.isBurrowed = false;
        enemy.isRetreating = false;
    }

    public override void DoExitLogic()
    {
        base.DoExitLogic();

        enemy.ForcedIdleDuration = enemy.PostAttackIdleDuration;
        enemy.ShouldRunAwayOnNextBurrow = Random.value < enemy.RunAwayChance;
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