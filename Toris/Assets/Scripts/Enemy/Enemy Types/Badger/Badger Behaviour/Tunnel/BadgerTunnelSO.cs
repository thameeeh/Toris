using UnityEngine;

[CreateAssetMenu(fileName = "Badger_Tunnel", menuName = "Enemy Logic/Tunnel Logic/Badger Tunnel")]
public class BadgerTunnelSO : TunnelSOBase<Badger>
{
    Vector2 _tunnelDirection;
    public float DistanceFromTargetPlayerPosition { get; set; }

    public override void Initialize(GameObject gameObject, Badger enemy, Transform player)
    {
        base.Initialize(gameObject, enemy, player);
    }

    public override void DoEnterLogic()
    {
        base.DoEnterLogic();

        DistanceFromTargetPlayerPosition = Vector2.Distance(enemy.TunnelLineTarget, enemy.transform.position);
        _tunnelDirection = (enemy.TunnelLineTarget - (Vector2)enemy.transform.position).normalized;
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

        Vector2 targetPosition = enemy.isRetreating ? enemy.RunAwayTargetPosition : (Vector2)enemy.PlayerTransform.position;

        _tunnelDirection = (enemy.TunnelLineTarget - (Vector2)enemy.transform.position).normalized;
        float currentSpeed = enemy.isRetreating ? enemy.TunnelingSpeed : enemy.LineTunnelingSpeed;

        enemy.MoveEnemy(_tunnelDirection * currentSpeed);
        DistanceFromTargetPlayerPosition = Vector2.Distance(enemy.TunnelLineTarget, enemy.transform.position);
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
