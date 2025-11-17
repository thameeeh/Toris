using UnityEngine;

[CreateAssetMenu(fileName = "Badger_Tunnel", menuName = "Enemy Logic/Tunnel Logic/Badger Tunnel")]
public class BadgerTunnelSO : TunnelSOBase<Badger>
{
    Vector2 _tunnelDirection;

    public override void Initialize(GameObject gameObject, Badger enemy, Transform player)
    {
        base.Initialize(gameObject, enemy, player);
    }

    public override void DoEnterLogic()
    {
        base.DoEnterLogic();

        _tunnelDirection = (enemy.TargetPlayerPosition - (Vector2)enemy.transform.position).normalized;
        enemy.MoveEnemy(_tunnelDirection);
        enemy.animator.Play("Tunnel BT");
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

        enemy.MoveEnemy(_tunnelDirection * enemy.TunnelingSpeed);
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
