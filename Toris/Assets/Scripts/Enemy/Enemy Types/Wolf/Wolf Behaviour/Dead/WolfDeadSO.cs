using UnityEditor.Experimental.GraphView;
using UnityEngine;

[CreateAssetMenu(fileName = "Wolf_Dead_Final", menuName = "Enemy Logic/Dead Logic/Wolf Dead Final")]
public class WolfDeadSO : DeadSOBase<Wolf>
{

    public override void Initialize(GameObject gameObject, Wolf enemy, Transform player)
    {
        base.Initialize(gameObject, enemy, player);
    }

    public override void DoEnterLogic()
    {
        base.DoEnterLogic();


        //Mini Wolf
        if (Inventory.InventoryInstance != null)
        {
            int coinAmount = enemy.role == WolfRole.Minion ? 3 : 5;
            Inventory.InventoryInstance.AddResourceStat(enemy._kill, 1);
            Inventory.InventoryInstance.AddResourceStat(enemy._coin, coinAmount);
        }

        enemy.animator.SetTrigger("Dead");
    }

    public override void DoExitLogic()
    {
        base.DoExitLogic();
    }

    public override void DoFrameUpdateLogic()
    {
        base.DoFrameUpdateLogic();

        enemy.MoveEnemy(Vector2.zero);
    }

    public override void DoPhysicsLogic()
    {
        base.DoPhysicsLogic();
    }

    public override void ResetValues()
    {
        base.ResetValues();
    }
    public override void DoAnimationTriggerEventLogic(Enemy.AnimationTriggerType triggerType)
    {
        base.DoAnimationTriggerEventLogic(triggerType);
    }
}
