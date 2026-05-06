using UnityEngine;

[CreateAssetMenu(fileName = "Wolf_Attack_QuickBite", menuName = "Enemy Logic/Attack Logic/Wolf Attack QuickBite")]
public class WolfAttackSO : AttackSOBase<Wolf>
{
    public bool isComplete {  get; private set; }

    private GridPathAgent _pathAgent;

    public override void Initialize(GameObject gameObject, Wolf enemy, Transform player)
    {
        base.Initialize(gameObject, enemy, player);

        _pathAgent = enemy.GetComponent<GridPathAgent>();
        if (_pathAgent == null)
        {
            Debug.LogWarning($"[WolfAttackSO] No GridPathAgent on {enemy.name}. Attack lunge will not use pathfinding.");
        }
    }

    public override void DoEnterLogic()
    {
        base.DoEnterLogic();

        isComplete = false;
    }

    public override void DoExitLogic()
    {
        base.DoExitLogic();
    }

    public override void DoFrameUpdateLogic()
    {
        base.DoFrameUpdateLogic();

        if (!enemy.IsMovingWhileBiting)
        {
            enemy.MoveEnemy(Vector2.zero);
            return;
        }

        Vector2 moveDirection = Vector2.zero;

        Transform targetTransform = enemy.AggroTargetTransform;
        if (_pathAgent != null && TileNavWorld.Instance != null && targetTransform != null)
            moveDirection = _pathAgent.GetMoveDirection(targetTransform.position);

        if (moveDirection.sqrMagnitude > 0.0001f)
            enemy.MoveEnemy(moveDirection * enemy.MovementSpeed);
        else
            enemy.MoveEnemy(Vector2.zero);
    }

    public override void DoPhysicsLogic()
    {
        base.DoPhysicsLogic();
    }

    public override void DoAnimationTriggerEventLogic(Enemy.AnimationTriggerType triggerType)
    {
        base.DoAnimationTriggerEventLogic(triggerType);

        if (triggerType == Enemy.AnimationTriggerType.Attack)
        {
            enemy.DamageCurrentTarget(enemy.AttackDamage);
        }

        if (triggerType == Enemy.AnimationTriggerType.AttackFinished)
        {
            isComplete = true;
        }
    }

    public override void ResetValues()
    {
        base.ResetValues();

        isComplete = false;
    }
}
