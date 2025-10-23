using UnityEngine;

[CreateAssetMenu(fileName = "Wolf_Howl_Alert", menuName = "Enemy Logic/Howl Logic/Wolf Howl Alert")]
public class WolfHowlSO : HowlSOBase<Wolf>
{
    private bool _hasHowled = false;
    public float HowlDuration = 1f;
    public override void Initialize(GameObject gameObject, Wolf enemy, Transform player)
    {
        base.Initialize(gameObject, enemy, player);
    }

    public override void DoEnterLogic()
    {
        base.DoEnterLogic();

        _hasHowled = true;
        enemy.animator.Play("Movement");
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

    public override void DoAnimationTriggerEventLogic(Wolf.AnimationTriggerType triggerType)
    {
        base.DoAnimationTriggerEventLogic(triggerType);
    }

    public override void ResetValues()
    {
        base.ResetValues();
    }
}
