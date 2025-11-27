using UnityEngine;

public class BadgerDeadState : EnemyState<Badger>
{
    private Vector2 _escapeDirection;
    private float _currentEscapeSpeed;
    public BadgerDeadState(Badger enemy, EnemyStateMachine enemyStateMachine)
        : base(enemy, enemyStateMachine) { }

    public override void EnterState()
    {
        base.EnterState();
        enemy.isBurrowed = true;
        enemy.isTunneling = true;

        enemy.MoveEnemy(Vector2.zero);
        enemy.animator.Play("Burrow BT");

        _escapeDirection = ((Vector2)enemy.transform.position - (Vector2)enemy.PlayerTransform.position).normalized;
        if (_escapeDirection == Vector2.zero)
        {
            _escapeDirection = Random.insideUnitCircle.normalized;
        }
        _currentEscapeSpeed = enemy.TunnelingSpeed;

        if (Inventory.InventoryInstance != null)
        {
            Inventory.InventoryInstance.AddResourceStat(enemy._kill, 1);
            Inventory.InventoryInstance.AddResourceStat(enemy._coin, 1);
        }
    }

    public override void ExitState()
    {
        base.ExitState();
    }

    public override void FrameUpdate()
    {
        base.FrameUpdate();
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();


        _currentEscapeSpeed += enemy.DeathBurrowAcceleration * Time.fixedDeltaTime;
        enemy.MoveEnemy(_escapeDirection * _currentEscapeSpeed);

        if (!enemy.IsVisibleOnScreen())
        {
            enemy.DestroyBadger();
        }
    }

    public override void AnimationTriggerEvent(Enemy.AnimationTriggerType triggerType)
    {
        base.AnimationTriggerEvent(triggerType);
    }
}