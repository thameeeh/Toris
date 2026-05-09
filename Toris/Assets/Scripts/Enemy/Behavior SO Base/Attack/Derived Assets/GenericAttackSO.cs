using UnityEngine;

[CreateAssetMenu(fileName = "Generic_Attack_Pew", menuName = "Enemy Logic/Generic/Generic Attack Pew")]
public class GenericAttackSO : AttackSOBase<Generic>
{

    public override void Initialize(GameObject gameObject, Generic enemy, Transform player)
    {
        base.Initialize(gameObject, enemy, player);
    }

    public override void DoEnterLogic()
    {
        base.DoEnterLogic();
#if UNITY_EDITOR
        enemy.DebugAttackLog($"Generic attack enter. {enemy.GetAttackDebugTargetSummary()}");
#endif
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
#if UNITY_EDITOR
        enemy.DebugAttackLog($"Generic attack animation event={triggerType} {enemy.GetAttackDebugTargetSummary()}");
#endif
    }

    public override void ResetValues()
    {
        base.ResetValues();
    }
}
