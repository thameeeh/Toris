using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Deer_RunAway", menuName = "Enemy Logic/Chase Logic/Deer Run Away")]
public class DeerRunAwaySO : ChaseSOBase<Deer>
{
    [SerializeField, Min(0.1f)] private float retargetInterval = 0.75f;
    [SerializeField, Min(0.1f)] private float fleeTargetDistance = 5f;
    [SerializeField, Min(0.01f)] private float targetTolerance = 0.25f;
    [SerializeField, Min(1)] private int maxCandidateChecks = 12;
    [SerializeField, Min(1)] private int maxCandidatePathRange = 25;
    [SerializeField, Min(1)] private int nearestWalkableSearchRadius = 6;
    [SerializeField, Range(0f, 180f)] private float fleeAngleSpread = 70f;
    [SerializeField, Range(0.1f, 1f)] private float minFleeDistanceMultiplier = 0.6f;
    [SerializeField, Min(0.1f)] private float directionLerpSpeed = 10f;
    [SerializeField, Min(0.0001f)] private float minMoveDirectionSqr = 0.0001f;

    private Vector2 fleeTarget;
    private Vector2 currentMoveDirection;
    private float nextRetargetTime;
    private GridPathAgent pathAgent;
    private readonly List<Vector3> candidatePath = new List<Vector3>();

    public override void Initialize(GameObject gameObject, Deer enemy, Transform player)
    {
        base.Initialize(gameObject, enemy, player);
        pathAgent = enemy.GetComponent<GridPathAgent>();

#if UNITY_EDITOR
        if (pathAgent == null)
            Debug.LogWarning($"[DeerRunAwaySO] No GridPathAgent on {enemy.name}. Deer flee can only direct-fallback when TileNavWorld is unavailable.");
#endif
    }

    public override void DoEnterLogic()
    {
        base.DoEnterLogic();

        enemy.BeginFearResponse();
        enemy.PlayRunAnimation();
        currentMoveDirection = Vector2.zero;
        nextRetargetTime = 0f;
        RetargetFlee();
    }

    public override void DoFrameUpdateLogic()
    {
        base.DoFrameUpdateLogic();

        if (!enemy.ShouldKeepFleeing)
        {
            enemy.StateMachine.ChangeState(enemy.IdleState);
            return;
        }

        Vector2 currentPosition = enemy.GetPosition2D();

        if (Time.time >= nextRetargetTime)
            RetargetFlee();

        Vector2 toTarget = fleeTarget - currentPosition;
        float targetToleranceSquared = targetTolerance * targetTolerance;

        if (toTarget.sqrMagnitude <= targetToleranceSquared)
        {
            RetargetFlee();
        }
    }

    public override void DoPhysicsLogic()
    {
        base.DoPhysicsLogic();

        if (enemy.StateMachine.CurrentEnemyState != enemy.RunAwayState)
            return;

        Vector2 desiredDirection = Vector2.zero;

        if (pathAgent != null)
            desiredDirection = pathAgent.GetMoveDirection(fleeTarget);

        if (desiredDirection.sqrMagnitude < minMoveDirectionSqr)
        {
            if (TileNavWorld.Instance != null && Time.time >= nextRetargetTime)
            {
                RetargetFlee();

                if (pathAgent != null)
                    desiredDirection = pathAgent.GetMoveDirection(fleeTarget);
            }
        }

        if (desiredDirection.sqrMagnitude < minMoveDirectionSqr)
        {
            Vector2 directDirection = fleeTarget - enemy.GetPosition2D();
            if (directDirection.sqrMagnitude > minMoveDirectionSqr && TileNavWorld.Instance == null)
                desiredDirection = directDirection.normalized;
        }

        if (desiredDirection.sqrMagnitude > minMoveDirectionSqr)
        {
            currentMoveDirection = Vector2.Lerp(
                currentMoveDirection,
                desiredDirection.normalized,
                directionLerpSpeed * Time.fixedDeltaTime);

            enemy.Run(currentMoveDirection.normalized);
        }
        else
        {
            currentMoveDirection = Vector2.zero;
            enemy.MoveEnemy(Vector2.zero);
            enemy.PlayRunAnimation();
        }
    }

    public override void DoExitLogic()
    {
        enemy.MoveEnemy(Vector2.zero);
        currentMoveDirection = Vector2.zero;
        base.DoExitLogic();
    }

    private void RetargetFlee()
    {
        Vector2 currentPosition = enemy.GetPosition2D();
        bool hasThreatPosition = enemy.TryGetFleeThreatPosition(out Vector2 threatPosition);
        Vector2 awayFromThreat = hasThreatPosition
            ? currentPosition - threatPosition
            : Random.insideUnitCircle;

        if (awayFromThreat.sqrMagnitude <= 0.0001f)
            awayFromThreat = Random.insideUnitCircle;

        if (awayFromThreat.sqrMagnitude <= 0.0001f)
            awayFromThreat = Vector2.down;

        Vector2 fleeDirection = awayFromThreat.normalized;
        fleeTarget = PickWalkableFleeTarget(currentPosition, fleeDirection, hasThreatPosition, threatPosition);
        nextRetargetTime = Time.time + retargetInterval;
    }

    private Vector2 PickWalkableFleeTarget(Vector2 currentPosition, Vector2 fleeDirection, bool hasThreatPosition, Vector2 threatPosition)
    {
        Vector2 desiredTarget = currentPosition + fleeDirection * fleeTargetDistance;
        TileNavWorld nav = TileNavWorld.Instance;

        if (nav == null)
            return desiredTarget;

        float bestScore = float.MinValue;
        Vector2 bestCandidate = desiredTarget;
        bool foundCandidate = false;

        for (int i = 0; i < maxCandidateChecks; i++)
        {
            Vector2 candidateDirection = i == 0
                ? fleeDirection
                : Rotate(fleeDirection, Random.Range(-fleeAngleSpread, fleeAngleSpread));

            float distance = Random.Range(fleeTargetDistance * minFleeDistanceMultiplier, fleeTargetDistance);
            Vector2 candidate = currentPosition + candidateDirection.normalized * distance;

            if (!nav.IsWalkableWorldPos(candidate) || !IsReachable(candidate))
                continue;

            float score = hasThreatPosition
                ? (candidate - threatPosition).sqrMagnitude
                : (candidate - currentPosition).sqrMagnitude;

            if (score <= bestScore)
                continue;

            bestScore = score;
            bestCandidate = candidate;
            foundCandidate = true;
        }

        if (foundCandidate)
            return bestCandidate;

        return FindNearestWalkablePoint(desiredTarget);
    }

    private Vector2 FindNearestWalkablePoint(Vector2 desiredWorldPosition)
    {
        TileNavWorld nav = TileNavWorld.Instance;
        if (nav == null)
            return desiredWorldPosition;

        Vector2Int startCell = nav.WorldToCell(desiredWorldPosition);

        if (nav.IsWalkableCell(startCell))
        {
            Vector2 worldCenter = nav.CellToWorldCenter(startCell);
            if (IsReachable(worldCenter))
                return worldCenter;
        }

        for (int radius = 1; radius <= nearestWalkableSearchRadius; radius++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                Vector2Int top = startCell + new Vector2Int(x, radius);
                if (TryResolveReachableCell(nav, top, out Vector2 topWorldPosition))
                    return topWorldPosition;

                Vector2Int bottom = startCell + new Vector2Int(x, -radius);
                if (TryResolveReachableCell(nav, bottom, out Vector2 bottomWorldPosition))
                    return bottomWorldPosition;
            }

            for (int y = -radius + 1; y <= radius - 1; y++)
            {
                Vector2Int right = startCell + new Vector2Int(radius, y);
                if (TryResolveReachableCell(nav, right, out Vector2 rightWorldPosition))
                    return rightWorldPosition;

                Vector2Int left = startCell + new Vector2Int(-radius, y);
                if (TryResolveReachableCell(nav, left, out Vector2 leftWorldPosition))
                    return leftWorldPosition;
            }
        }

        return enemy.GetPosition2D();
    }

    private bool TryResolveReachableCell(TileNavWorld nav, Vector2Int cell, out Vector2 worldPosition)
    {
        worldPosition = default;
        if (!nav.IsWalkableCell(cell))
            return false;

        worldPosition = nav.CellToWorldCenter(cell);
        return IsReachable(worldPosition);
    }

    private bool IsReachable(Vector2 worldPosition)
    {
        TileNavWorld nav = TileNavWorld.Instance;
        if (nav == null)
            return true;

        Vector2 currentPosition = enemy.GetPosition2D();
        if (!nav.IsWalkableWorldPos(currentPosition))
            return false;

        return TilePathfinder.TryFindPath(
            currentPosition,
            worldPosition,
            candidatePath,
            maxCandidatePathRange);
    }

    private static Vector2 Rotate(Vector2 direction, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);

        return new Vector2(
            direction.x * cos - direction.y * sin,
            direction.x * sin + direction.y * cos);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        retargetInterval = Mathf.Max(0.1f, retargetInterval);
        fleeTargetDistance = Mathf.Max(0.1f, fleeTargetDistance);
        targetTolerance = Mathf.Max(0.01f, targetTolerance);
        maxCandidateChecks = Mathf.Max(1, maxCandidateChecks);
        maxCandidatePathRange = Mathf.Max(1, maxCandidatePathRange);
        nearestWalkableSearchRadius = Mathf.Max(1, nearestWalkableSearchRadius);
        minFleeDistanceMultiplier = Mathf.Clamp(minFleeDistanceMultiplier, 0.1f, 1f);
        directionLerpSpeed = Mathf.Max(0.1f, directionLerpSpeed);
        minMoveDirectionSqr = Mathf.Max(0.0001f, minMoveDirectionSqr);
    }
#endif
}
