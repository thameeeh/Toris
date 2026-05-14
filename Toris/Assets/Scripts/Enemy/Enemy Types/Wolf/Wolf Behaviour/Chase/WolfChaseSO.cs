using UnityEngine;

public class WolfChaseSO : ChaseSOBase<Wolf>
{
    [Header("Ranges")]
    [Tooltip("Stop moving closer when within this distance to the player (prevents micro jitter in melee).")]
    [SerializeField] private float _stopDistance = 1.0f;

    private GridPathAgent _pathAgent;

    public override void Initialize(GameObject gameObject, Wolf enemy, Transform player)
    {
        base.Initialize(gameObject, enemy, player);

        _pathAgent = enemy.GetComponent<GridPathAgent>();
        if (_pathAgent == null)
        {
            Debug.LogWarning($"[WolfChaseSO] No GridPathAgent on {enemy.name}. Chase will not use pathfinding.");
        }
    }

    public override void DoEnterLogic()
    {
        base.DoEnterLogic();

        enemy.SetChasingPlayer(true);

        enemy.animator.SetBool("IsMoving", true);
        enemy.animator.Play("Run");
    }

    public override void DoFrameUpdateLogic()
    {
        base.DoFrameUpdateLogic();

        Transform targetTransform = enemy.AggroTargetTransform;
        if (!TryResolveChaseTarget(out Vector2 targetPos, targetTransform))
        {
            enemy.animator.SetBool("IsMoving", false);
            enemy.MoveEnemy(Vector2.zero);
            return;
        }

        Vector2 wolfPos = enemy.transform.position;
        Vector2 toTarget = targetPos - wolfPos;

        float distToTarget = toTarget.magnitude;

        bool canHoldBitePosition = enemy.IsWithinStrikingDistance && distToTarget <= _stopDistance;

        // 1. Only stop if the wolf is actually in striking range.
        // Otherwise it can park just outside the bite trigger and appear to run in place.
        if (canHoldBitePosition)
        {
            enemy.animator.SetBool("IsMoving", false);
            enemy.MoveEnemy(Vector2.zero);
            return;
        }

        Vector2 moveDir = Vector2.zero;

        if (_pathAgent != null && TileNavWorld.Instance != null)
        {
            moveDir = _pathAgent.GetMoveDirection(targetPos);
        }
        else
        {
            moveDir = distToTarget > 0.0001f ? (toTarget / distToTarget) : Vector2.zero;
        }

        float speed = enemy.MovementSpeed;

        if (moveDir.sqrMagnitude > 0.0001f)
        {
            enemy.animator.SetBool("IsMoving", true);
            enemy.MoveEnemy(moveDir * speed);
        }
        else
        {
            enemy.animator.SetBool("IsMoving", false);
            enemy.MoveEnemy(Vector2.zero);
        }
    }

    public override void DoExitLogic() 
    { 
        base.DoExitLogic();
        
        enemy.SetChasingPlayer(false);
    }
    public override void DoPhysicsLogic() { base.DoPhysicsLogic(); }
    public override void DoAnimationTriggerEventLogic(Enemy.AnimationTriggerType triggerType) { base.DoAnimationTriggerEventLogic(triggerType); }
    public override void ResetValues() { base.ResetValues(); }

    private bool TryResolveChaseTarget(out Vector2 targetPos, Transform targetTransform)
    {
        if (targetTransform != null)
        {
            targetPos = targetTransform.position;
            return true;
        }

        if (enemy.ShouldRemainInChase() && enemy.TryGetLastKnownAggroTargetPosition(out targetPos))
            return true;

        targetPos = default;
        return false;
    }
}
