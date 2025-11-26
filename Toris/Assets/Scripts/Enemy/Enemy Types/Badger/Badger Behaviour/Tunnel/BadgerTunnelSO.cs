using UnityEngine;

[CreateAssetMenu(fileName = "Badger_Tunnel", menuName = "Enemy Logic/Tunnel Logic/Badger Tunnel")]
public class BadgerTunnelSO : TunnelSOBase<Badger>
{
    private Vector2 _tunnelDirection;
    public float DistanceFromTargetPlayerPosition { get; private set; }
    [SerializeField] private float stopDistance = 0.25f;

    public override void Initialize(GameObject gameObject, Badger enemy, Transform player)
    {
        base.Initialize(gameObject, enemy, player);
    }

    public override void DoEnterLogic()
    {
        base.DoEnterLogic();

        _tunnelDirection = (enemy.TunnelLineTarget - (Vector2)enemy.transform.position).normalized;
        if (_tunnelDirection == Vector2.zero)
        {
            _tunnelDirection = Vector2.right;
        }

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

        _tunnelDirection = (enemy.TunnelLineTarget - (Vector2)enemy.transform.position).normalized;
        if (_tunnelDirection == Vector2.zero)
        {
            _tunnelDirection = Vector2.right;
        }

        float speed = enemy.LineTunnelingSpeed;
        enemy.MoveEnemy(_tunnelDirection * speed);

        DistanceFromTargetPlayerPosition =
            Vector2.Distance(enemy.TunnelLineTarget, enemy.transform.position);
    }
    public override void DoAnimationTriggerEventLogic(Enemy.AnimationTriggerType triggerType)
    {
        base.DoAnimationTriggerEventLogic(triggerType);
    }

    public override void ResetValues()
    {
        base.ResetValues();
        DistanceFromTargetPlayerPosition = 0f;
    }
}
