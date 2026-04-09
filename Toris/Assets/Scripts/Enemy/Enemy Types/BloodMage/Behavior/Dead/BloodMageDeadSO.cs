using UnityEngine;

[CreateAssetMenu(fileName = "BloodMage_Dead_Final", menuName = "Enemy Logic/Dead Logic/BloodMage Dead Final")]
public class BloodMageDeadSO : DeadSOBase<BloodMage>
{
    public override void DoEnterLogic()
    {
        base.DoEnterLogic();
        enemy.MoveEnemy(Vector2.zero);
        enemy.SetMovementAnimation(false);
        enemy.RequestDeathAnimation();
    }

    public override void DoFrameUpdateLogic()
    {
        base.DoFrameUpdateLogic();
        enemy.MoveEnemy(Vector2.zero);
    }

    public override void DoPhysicsLogic()
    {
        base.DoPhysicsLogic();
        enemy.MoveEnemy(Vector2.zero);
    }
}
