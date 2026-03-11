using UnityEngine;

[CreateAssetMenu(fileName = "Wolf_Idle_Wander", menuName = "Enemy Logic/Idle Logic/Wolf Idle Wander")]
public class WolfIdleSO : IdleSOBase<Wolf>
{
    [Header("Idle Timing")]
    [SerializeField] private float wanderInterval = 3f;
    [SerializeField] private float idlePauseDuration = 0.35f;

    [Header("Home Wander")]
    [SerializeField] private float homeRadiusUsage = 0.8f;
    [SerializeField] private float outwardPadding = 0.5f;
    [SerializeField] private float returnToHomeThreshold = 0.25f;
    [SerializeField] private int maxCandidateChecks = 12;

    [Header("Target Filtering")]
    [SerializeField] private float minTargetDistanceFromCurrent = 1.25f;

    [Header("Arrival")]
    [Tooltip("Squared distance threshold to decide we've reached the wander point.")]
    [SerializeField] private float wanderPointReachSqr = 0.25f;

    [Header("Steering")]
    [Tooltip("How quickly the wolf turns towards a new path direction. Higher = snappier.")]
    [SerializeField] private float directionLerpSpeed = 4f;
    [SerializeField] private float minMoveDirectionSqr = 0.0001f;

    private float movementSpeed;
    private float timer;
    private float idlePauseTimer;
    private Vector2 wanderPoint;
    private Vector2 currentMoveDir;
    private GridPathAgent pathAgent;
    private bool isPausedAtPoint;

    public override void Initialize(GameObject gameObject, Wolf enemy, Transform player)
    {
        base.Initialize(gameObject, enemy, player);

        pathAgent = enemy.GetComponent<GridPathAgent>();
        if (pathAgent == null)
        {
            Debug.LogWarning($"[WolfIdleSO] No GridPathAgent on {enemy.name}. Idle wander will not use pathfinding.");
        }

        timer = wanderInterval;
        idlePauseTimer = 0f;
        wanderPoint = enemy.transform.position;
        currentMoveDir = Vector2.zero;
        isPausedAtPoint = false;
    }

    public override void DoEnterLogic()
    {
        base.DoEnterLogic();

        movementSpeed = enemy.MovementSpeed;
        timer = wanderInterval;
        idlePauseTimer = 0f;
        wanderPoint = enemy.transform.position;
        currentMoveDir = Vector2.zero;
        isPausedAtPoint = false;

        enemy.animator.SetBool("IsMoving", false);
    }

    public override void DoExitLogic()
    {
        base.DoExitLogic();

        enemy.animator.SetBool("IsMoving", false);
        enemy.MoveEnemy(Vector2.zero);
        currentMoveDir = Vector2.zero;
        idlePauseTimer = 0f;
        isPausedAtPoint = false;
    }

    public override void DoFrameUpdateLogic()
    {
        base.DoFrameUpdateLogic();

        if (isPausedAtPoint)
        {
            idlePauseTimer -= Time.deltaTime;

            if (idlePauseTimer <= 0f)
            {
                wanderPoint = GetNextIdleTarget();
                timer = 0f;
                isPausedAtPoint = false;
            }

            return;
        }

        timer += Time.deltaTime;

        Vector2 currentPos = enemy.transform.position;
        float sqrDistToWander = (wanderPoint - currentPos).sqrMagnitude;

        if (sqrDistToWander <= wanderPointReachSqr)
        {
            isPausedAtPoint = true;
            idlePauseTimer = idlePauseDuration;
            return;
        }

        if (timer >= wanderInterval)
        {
            wanderPoint = GetNextIdleTarget();
            timer = 0f;
        }
    }

    public override void DoPhysicsLogic()
    {
        base.DoPhysicsLogic();

        if (isPausedAtPoint)
        {
            currentMoveDir = Vector2.zero;
            enemy.MoveEnemy(Vector2.zero);
            enemy.animator.SetBool("IsMoving", false);
            return;
        }

        Vector2 currentPos = enemy.transform.position;
        Vector2 desiredDir = Vector2.zero;

        if (pathAgent != null)
        {
            desiredDir = pathAgent.GetMoveDirection(wanderPoint);
        }

        if (desiredDir.sqrMagnitude < minMoveDirectionSqr)
        {
            Vector2 direct = wanderPoint - currentPos;
            if (direct.sqrMagnitude > minMoveDirectionSqr)
                desiredDir = direct.normalized;
        }

        if (desiredDir.sqrMagnitude > minMoveDirectionSqr)
        {
            currentMoveDir = Vector2.Lerp(
                currentMoveDir,
                desiredDir.normalized,
                directionLerpSpeed * Time.fixedDeltaTime
            );

            enemy.MoveEnemy(currentMoveDir.normalized * movementSpeed);
            enemy.animator.SetBool("IsMoving", true);
        }
        else
        {
            enemy.MoveEnemy(Vector2.zero);
            enemy.animator.SetBool("IsMoving", false);
        }
    }

    public override void DoAnimationTriggerEventLogic(Enemy.AnimationTriggerType triggerType)
    {
        base.DoAnimationTriggerEventLogic(triggerType);
    }

    public override void ResetValues()
    {
        base.ResetValues();

        timer = 0f;
        idlePauseTimer = 0f;
        currentMoveDir = Vector2.zero;
        wanderPoint = enemy.transform.position;
        isPausedAtPoint = false;
    }

    private Vector2 GetNextIdleTarget()
    {
        Vector2 currentPos = enemy.transform.position;
        Vector2 homeCenter = enemy.HomeCenter;
        float homeRadius = enemy.HomeRadius;

        if (!enemy.HasHome)
            return GetWalkablePointNearOrigin(currentPos, homeRadius);

        float returnThresholdDistance = homeRadius + returnToHomeThreshold;
        float distanceToHome = Vector2.Distance(currentPos, homeCenter);

        if (distanceToHome > returnThresholdDistance)
        {
            return GetWalkablePointNearOrigin(homeCenter, Mathf.Max(0.1f, homeRadius * homeRadiusUsage));
        }

        float usableRadius = Mathf.Max(0.1f, homeRadius * homeRadiusUsage - outwardPadding);
        return GetWalkablePointNearOrigin(homeCenter, usableRadius);
    }

    private Vector2 GetWalkablePointNearOrigin(Vector2 origin, float radius)
    {
        float minTargetDistanceSqr = minTargetDistanceFromCurrent * minTargetDistanceFromCurrent;
        Vector2 currentPos = enemy.transform.position;

        if (TileNavWorld.Instance == null)
        {
            for (int i = 0; i < maxCandidateChecks; i++)
            {
                Vector2 candidate = origin + Random.insideUnitCircle * radius;

                if ((candidate - currentPos).sqrMagnitude < minTargetDistanceSqr)
                    continue;

                return candidate;
            }

            return origin;
        }

        for (int i = 0; i < maxCandidateChecks; i++)
        {
            Vector2 candidate = origin + Random.insideUnitCircle * radius;

            if ((candidate - currentPos).sqrMagnitude < minTargetDistanceSqr)
                continue;

            if (TileNavWorld.Instance.IsWalkableWorldPos(candidate))
                return candidate;
        }

        return FindNearestWalkablePoint(origin);
    }

    private Vector2 FindNearestWalkablePoint(Vector2 desiredWorldPos)
    {
        var nav = TileNavWorld.Instance;
        if (nav == null)
            return desiredWorldPos;

        Vector2Int startCell = nav.WorldToCell(desiredWorldPos);

        if (nav.IsWalkableCell(startCell))
            return nav.CellToWorldCenter(startCell);

        int searchRadius = Mathf.Max(1, maxCandidateChecks / 2);

        for (int r = 1; r <= searchRadius; r++)
        {
            for (int x = -r; x <= r; x++)
            {
                Vector2Int top = startCell + new Vector2Int(x, r);
                if (nav.IsWalkableCell(top))
                    return nav.CellToWorldCenter(top);

                Vector2Int bottom = startCell + new Vector2Int(x, -r);
                if (nav.IsWalkableCell(bottom))
                    return nav.CellToWorldCenter(bottom);
            }

            for (int y = -r + 1; y <= r - 1; y++)
            {
                Vector2Int right = startCell + new Vector2Int(r, y);
                if (nav.IsWalkableCell(right))
                    return nav.CellToWorldCenter(right);

                Vector2Int left = startCell + new Vector2Int(-r, y);
                if (nav.IsWalkableCell(left))
                    return nav.CellToWorldCenter(left);
            }
        }

        return enemy.transform.position;
    }
}