using UnityEngine;

[CreateAssetMenu(fileName = "Necromancer_Dead_Final", menuName = "Enemy Logic/Dead Logic/Necromancer Dead Final")]
public class NecromancerDeadSO : DeadSOBase<Necromancer>
{
    public override void DoEnterLogic()
    {
        base.DoEnterLogic();

        enemy.MoveEnemy(Vector2.zero);
        enemy.SetMovementAnimation(false);
#if UNITY_EDITOR
        enemy.DebugAnimationLog("DeadSO enter -> setting Dead trigger.");
#endif
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
