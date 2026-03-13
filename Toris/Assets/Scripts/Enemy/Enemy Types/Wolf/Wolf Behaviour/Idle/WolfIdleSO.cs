using OutlandHaven.UIToolkit;
using UnityEngine;

[CreateAssetMenu(fileName = "Wolf_Idle_Wander", menuName = "Enemy Logic/Idle Logic/Wolf Idle Wander")]
public class WolfIdleSO : IdleSOBase<Wolf>
{
    private enum IdleMode
    {
        StandRest,
        PatrolHome,
        Regroup,
        Investigate
    }

    [Header("Idle Mode Timing")]
    [SerializeField] private float decisionInterval = 0.1f;
    [SerializeField] private float leaderRestDurationMin = 1.0f;
    [SerializeField] private float leaderRestDurationMax = 2.0f;
    [SerializeField] private float minionRestDurationMin = 1.5f;
    [SerializeField] private float minionRestDurationMax = 3.0f;

    [Header("Idle Mode Weights")]
    [Range(0f, 1f)]
    [SerializeField] private float leaderRestChance = 0.35f;
    [Range(0f, 1f)]
    [SerializeField] private float minionRestChance = 0.65f;

    [Header("Patrol Timing")]
    [SerializeField] private float wanderInterval = 3f;
    [SerializeField] private float idlePauseDuration = 0.35f;

    [Header("Home Wander")]
    [SerializeField] private float homeRadiusUsage = 0.8f;
    [SerializeField] private float outwardPadding = 0.5f;
    [SerializeField] private float returnToHomeThreshold = 0.25f;
    [SerializeField] private int maxCandidateChecks = 12;

    [Header("Regroup")]
    [SerializeField] private float regroupThresholdPadding = 0.5f;
    [SerializeField] private float regroupArrivePadding = 0.25f;
    [SerializeField] private float regroupHomeRadiusUsage = 0.55f;

    [Header("Investigate")]
    [SerializeField] private float investigateArriveSqr = 0.35f;
    [SerializeField] private float investigateRepathInterval = 0.35f;
    [SerializeField] private float investigateStandDurationMin = 1.0f;
    [SerializeField] private float investigateStandDurationMax = 2.0f;

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
    private float patrolTimer;
    private float idlePauseTimer;
    private float modeTimer;
    private float decisionTimer;
    private float investigateRepathTimer;
    private float investigateStandTimer;
    private float investigateRetargetSqr = 0.25f;

    private Vector2 wanderPoint;
    private Vector2 currentMoveDir;
    private Vector2 committedInvestigateTarget;
    private GridPathAgent pathAgent;
    private EnemyAlertIndicator alertIndicator;

    private bool isPausedAtPoint;
    private bool isStandingAtInvestigatePoint;
    private IdleMode currentMode;

    public override void Initialize(GameObject gameObject, Wolf enemy, Transform player)
    {
        base.Initialize(gameObject, enemy, player);

        pathAgent = enemy.GetComponent<GridPathAgent>();
        if (pathAgent == null)
        {
            Debug.LogWarning($"[WolfIdleSO] No GridPathAgent on {enemy.name}. Idle wander will not use pathfinding.");
        }

        alertIndicator = enemy.GetComponent<EnemyAlertIndicator>();

        patrolTimer = wanderInterval;
        idlePauseTimer = 0f;
        modeTimer = 0f;
        decisionTimer = 0f;
        investigateRepathTimer = 0f;
        investigateStandTimer = 0f;

        wanderPoint = enemy.transform.position;
        committedInvestigateTarget = enemy.transform.position;
        currentMoveDir = Vector2.zero;
        isPausedAtPoint = false;
        isStandingAtInvestigatePoint = false;
        currentMode = IdleMode.StandRest;
    }

    public override void DoEnterLogic()
    {
        base.DoEnterLogic();

        movementSpeed = enemy.MovementSpeed;
        patrolTimer = wanderInterval;
        idlePauseTimer = 0f;
        modeTimer = 0f;
        decisionTimer = 0f;
        investigateRepathTimer = 0f;
        investigateStandTimer = 0f;

        wanderPoint = enemy.transform.position;
        committedInvestigateTarget = enemy.transform.position;
        currentMoveDir = Vector2.zero;
        isPausedAtPoint = false;
        isStandingAtInvestigatePoint = false;

        ChooseNewMode(force: true);

        enemy.animator.SetBool("IsMoving", false);
    }

    public override void DoExitLogic()
    {
        base.DoExitLogic();

        enemy.animator.SetBool("IsMoving", false);
        enemy.MoveEnemy(Vector2.zero);

        currentMoveDir = Vector2.zero;
        idlePauseTimer = 0f;
        modeTimer = 0f;
        decisionTimer = 0f;
        investigateRepathTimer = 0f;
        investigateStandTimer = 0f;
        isPausedAtPoint = false;
        isStandingAtInvestigatePoint = false;

        if (alertIndicator != null)
            alertIndicator.HideIndicator();
    }

    public override void DoFrameUpdateLogic()
    {
        base.DoFrameUpdateLogic();

        decisionTimer -= Time.deltaTime;
        modeTimer -= Time.deltaTime;

        if (currentMode == IdleMode.Investigate)
        {
            UpdateInvestigateMode();
            return;
        }

        if (enemy.IsInvestigationTargetActive())
        {
            EnterInvestigateMode();
            return;
        }

        if (currentMode != IdleMode.Regroup && ShouldRegroup())
        {
            EnterRegroupMode();
            return;
        }

        if (currentMode == IdleMode.StandRest)
        {
            enemy.animator.SetBool("IsMoving", false);

            if (modeTimer <= 0f && decisionTimer <= 0f)
            {
                ChooseNewMode(force: false);
            }

            return;
        }

        if (currentMode == IdleMode.Regroup)
        {
            if (HasFinishedRegroup())
            {
                ChooseNewMode(force: true);
                return;
            }

            float sqrDistToRegroupTarget = (wanderPoint - (Vector2)enemy.transform.position).sqrMagnitude;
            if (sqrDistToRegroupTarget <= wanderPointReachSqr)
            {
                wanderPoint = GetRegroupTarget();
            }

            return;
        }

        if (isPausedAtPoint)
        {
            idlePauseTimer -= Time.deltaTime;

            if (idlePauseTimer <= 0f)
            {
                if (decisionTimer <= 0f)
                {
                    ChooseNewMode(force: false);
                }

                if (currentMode == IdleMode.PatrolHome)
                {
                    wanderPoint = GetNextIdleTarget();
                    patrolTimer = 0f;
                    isPausedAtPoint = false;
                }
            }

            return;
        }

        patrolTimer += Time.deltaTime;

        Vector2 currentPos = enemy.transform.position;
        float sqrDistToWander = (wanderPoint - currentPos).sqrMagnitude;

        if (sqrDistToWander <= wanderPointReachSqr)
        {
            isPausedAtPoint = true;
            idlePauseTimer = idlePauseDuration;
            currentMoveDir = Vector2.zero;
            return;
        }

        if (patrolTimer >= wanderInterval)
        {
            wanderPoint = GetNextIdleTarget();
            patrolTimer = 0f;
        }

        if (modeTimer <= 0f && decisionTimer <= 0f)
        {
            ChooseNewMode(force: false);
        }
    }

    public override void DoPhysicsLogic()
    {
        base.DoPhysicsLogic();

        if (currentMode == IdleMode.StandRest || isPausedAtPoint || isStandingAtInvestigatePoint)
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

        patrolTimer = 0f;
        idlePauseTimer = 0f;
        modeTimer = 0f;
        decisionTimer = 0f;
        investigateRepathTimer = 0f;
        investigateStandTimer = 0f;

        currentMoveDir = Vector2.zero;
        wanderPoint = enemy.transform.position;
        committedInvestigateTarget = enemy.transform.position;
        isPausedAtPoint = false;
        isStandingAtInvestigatePoint = false;
        currentMode = IdleMode.StandRest;
    }

    private void UpdateInvestigateMode()
    {
        if (enemy.IsInvestigationTargetActive())
        {
            Vector2 latestTarget = enemy.InvestigationTarget;

            // If a new alert moved the target meaningfully, break out of the stand
            // and continue investigating the new point.
            if ((latestTarget - committedInvestigateTarget).sqrMagnitude > investigateRetargetSqr)
            {
                committedInvestigateTarget = latestTarget;
                wanderPoint = committedInvestigateTarget;
                investigateRepathTimer = investigateRepathInterval;
                isStandingAtInvestigatePoint = false;
                investigateStandTimer = 0f;
                currentMoveDir = Vector2.zero;
            }
        }

        if (!enemy.IsInvestigationTargetActive())
        {
            if (isStandingAtInvestigatePoint)
            {
                investigateStandTimer -= Time.deltaTime;
                if (investigateStandTimer <= 0f)
                {
                    CompleteInvestigation();
                }
            }
            else
            {
                CompleteInvestigation();
            }

            return;
        }

        if (isStandingAtInvestigatePoint)
        {
            investigateStandTimer -= Time.deltaTime;
            if (investigateStandTimer <= 0f)
            {
                enemy.ClearInvestigationTarget();
                CompleteInvestigation();
            }

            return;
        }

        investigateRepathTimer -= Time.deltaTime;
        if (investigateRepathTimer <= 0f)
        {
            wanderPoint = committedInvestigateTarget;
            investigateRepathTimer = investigateRepathInterval;
        }

        float sqrDistToInvestigate = (wanderPoint - (Vector2)enemy.transform.position).sqrMagnitude;
        if (sqrDistToInvestigate <= investigateArriveSqr)
        {
            isStandingAtInvestigatePoint = true;
            investigateStandTimer =
                Random.Range(investigateStandDurationMin, investigateStandDurationMax)
                + enemy.InvestigationStandDurationBonus;

            currentMoveDir = Vector2.zero;
            enemy.MoveEnemy(Vector2.zero);
            enemy.animator.SetBool("IsMoving", false);
        }
    }

    private void CompleteInvestigation()
    {
        isStandingAtInvestigatePoint = false;

        if (alertIndicator != null)
            alertIndicator.HideIndicator();

        decisionTimer = 0f;
        ChooseNewMode(force: true);
    }

    private void ChooseNewMode(bool force)
    {
        decisionTimer = decisionInterval;

        if (enemy.IsInvestigationTargetActive())
        {
            EnterInvestigateMode();
            return;
        }

        if (ShouldRegroup())
        {
            EnterRegroupMode();
            return;
        }

        float restChance = enemy.role == WolfRole.Leader ? leaderRestChance : minionRestChance;

        IdleMode nextMode = Random.value < restChance
            ? IdleMode.StandRest
            : IdleMode.PatrolHome;

        if (!force && nextMode == currentMode)
        {
            modeTimer = GetModeDuration(currentMode);
            return;
        }

        currentMode = nextMode;
        modeTimer = GetModeDuration(currentMode);

        isPausedAtPoint = false;
        isStandingAtInvestigatePoint = false;

        if (alertIndicator != null)
            alertIndicator.HideIndicator();

        if (currentMode == IdleMode.StandRest)
        {
            currentMoveDir = Vector2.zero;
            enemy.MoveEnemy(Vector2.zero);
            enemy.animator.SetBool("IsMoving", false);
        }
        else
        {
            wanderPoint = GetNextIdleTarget();
            patrolTimer = 0f;
        }
    }

    private void EnterRegroupMode()
    {
        currentMode = IdleMode.Regroup;
        modeTimer = wanderInterval;
        isPausedAtPoint = false;
        isStandingAtInvestigatePoint = false;
        currentMoveDir = Vector2.zero;
        wanderPoint = GetRegroupTarget();
        patrolTimer = 0f;

        if (alertIndicator != null)
            alertIndicator.HideIndicator();
    }

    private void EnterInvestigateMode()
    {
        currentMode = IdleMode.Investigate;
        modeTimer = wanderInterval;
        isPausedAtPoint = false;
        isStandingAtInvestigatePoint = false;
        currentMoveDir = Vector2.zero;

        committedInvestigateTarget = enemy.InvestigationTarget;
        wanderPoint = committedInvestigateTarget;
        investigateRepathTimer = investigateRepathInterval;
        investigateStandTimer = 0f;
        patrolTimer = 0f;

        if (alertIndicator != null)
            alertIndicator.ShowPersistent();
    }

    private float GetModeDuration(IdleMode mode)
    {
        switch (mode)
        {
            case IdleMode.StandRest:
                if (enemy.role == WolfRole.Leader)
                    return Random.Range(leaderRestDurationMin, leaderRestDurationMax);

                return Random.Range(minionRestDurationMin, minionRestDurationMax);

            case IdleMode.PatrolHome:
            case IdleMode.Regroup:
            case IdleMode.Investigate:
                return wanderInterval;

            default:
                return wanderInterval;
        }
    }

    private bool ShouldRegroup()
    {
        if (!enemy.HasHome)
            return false;

        float regroupThreshold = enemy.HomeRadius + regroupThresholdPadding;
        float distanceToHome = Vector2.Distance(enemy.transform.position, enemy.HomeCenter);

        return distanceToHome > regroupThreshold;
    }

    private bool HasFinishedRegroup()
    {
        if (!enemy.HasHome)
            return true;

        float arriveDistance = Mathf.Max(0.1f, enemy.HomeRadius - regroupArrivePadding);
        float distanceToHome = Vector2.Distance(enemy.transform.position, enemy.HomeCenter);

        return distanceToHome <= arriveDistance;
    }

    private Vector2 GetRegroupTarget()
    {
        Vector2 homeCenter = enemy.HomeCenter;
        float radius = Mathf.Max(0.1f, enemy.HomeRadius * regroupHomeRadiusUsage);
        return GetWalkablePointNearOrigin(homeCenter, radius);
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

        for (int iRadius = 1; iRadius <= searchRadius; iRadius++)
        {
            for (int x = -iRadius; x <= iRadius; x++)
            {
                Vector2Int top = startCell + new Vector2Int(x, iRadius);
                if (nav.IsWalkableCell(top))
                    return nav.CellToWorldCenter(top);

                Vector2Int bottom = startCell + new Vector2Int(x, -iRadius);
                if (nav.IsWalkableCell(bottom))
                    return nav.CellToWorldCenter(bottom);
            }

            for (int y = -iRadius + 1; y <= iRadius - 1; y++)
            {
                Vector2Int right = startCell + new Vector2Int(iRadius, y);
                if (nav.IsWalkableCell(right))
                    return nav.CellToWorldCenter(right);

                Vector2Int left = startCell + new Vector2Int(-iRadius, y);
                if (nav.IsWalkableCell(left))
                    return nav.CellToWorldCenter(left);
            }
        }

        return enemy.transform.position;
    }
}