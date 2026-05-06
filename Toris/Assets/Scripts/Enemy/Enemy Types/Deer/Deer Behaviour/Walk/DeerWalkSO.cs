using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Deer_Walk", menuName = "Enemy Logic/Walk Logic/Deer Walk")]
public class DeerWalkSO : WalkSOBase<Deer>
{
    [SerializeField, Min(0.1f)] private float wanderRadius = 4f;
    [SerializeField, Min(0.01f)] private float destinationTolerance = 0.2f;
    [SerializeField, Min(1)] private int maxCandidateChecks = 12;
    [SerializeField, Min(1)] private int maxCandidatePathRange = 25;
    [SerializeField, Min(1)] private int nearestWalkableSearchRadius = 6;
    [SerializeField, Min(0f)] private float minTargetDistanceFromCurrent = 1f;
    [SerializeField, Min(0.1f)] private float directionLerpSpeed = 6f;
    [SerializeField, Min(0.0001f)] private float minMoveDirectionSqr = 0.0001f;

    private Vector2 targetPosition;
    private Vector2 currentMoveDirection;
    private GridPathAgent pathAgent;
    private readonly List<Vector3> candidatePath = new List<Vector3>();

    public override void Initialize(GameObject gameObject, Deer enemy, Transform player)
    {
        base.Initialize(gameObject, enemy, player);
        pathAgent = enemy.GetComponent<GridPathAgent>();

#if UNITY_EDITOR
        if (pathAgent == null)
            Debug.LogWarning($"[DeerWalkSO] No GridPathAgent on {enemy.name}. Deer walk can only direct-fallback when TileNavWorld is unavailable.");
#endif
    }

    public override void DoEnterLogic()
    {
        base.DoEnterLogic();

        Vector2 currentPosition = enemy.GetPosition2D();
        targetPosition = GetWalkablePointNearOrigin(currentPosition, wanderRadius);
        currentMoveDirection = Vector2.zero;
        enemy.PlayWalkAnimation();
    }

    public override void DoFrameUpdateLogic()
    {
        base.DoFrameUpdateLogic();

        if (enemy.IsAggroed)
        {
            enemy.StateMachine.ChangeState(enemy.RunAwayState);
            return;
        }

        Vector2 currentPosition = enemy.GetPosition2D();
        float toleranceSquared = destinationTolerance * destinationTolerance;

        if ((targetPosition - currentPosition).sqrMagnitude <= toleranceSquared)
        {
            enemy.StateMachine.ChangeState(enemy.IdleState);
            return;
        }
    }

    public override void DoPhysicsLogic()
    {
        base.DoPhysicsLogic();

        if (enemy.StateMachine.CurrentEnemyState != enemy.WalkState)
            return;

        Vector2 desiredDirection = Vector2.zero;

        if (pathAgent != null)
            desiredDirection = pathAgent.GetMoveDirection(targetPosition);

        if (desiredDirection.sqrMagnitude < minMoveDirectionSqr)
        {
            Vector2 directDirection = targetPosition - enemy.GetPosition2D();
            if (directDirection.sqrMagnitude > minMoveDirectionSqr && TileNavWorld.Instance == null)
                desiredDirection = directDirection.normalized;
        }

        if (desiredDirection.sqrMagnitude > minMoveDirectionSqr)
        {
            currentMoveDirection = Vector2.Lerp(
                currentMoveDirection,
                desiredDirection.normalized,
                directionLerpSpeed * Time.fixedDeltaTime);

            enemy.Walk(currentMoveDirection.normalized);
        }
        else
        {
            currentMoveDirection = Vector2.zero;
            enemy.MoveEnemy(Vector2.zero);
            enemy.PlayIdleAnimation();

            if (TileNavWorld.Instance != null)
                enemy.StateMachine.ChangeState(enemy.IdleState);
        }
    }

    public override void DoExitLogic()
    {
        enemy.MoveEnemy(Vector2.zero);
        currentMoveDirection = Vector2.zero;
        base.DoExitLogic();
    }

    private Vector2 GetWalkablePointNearOrigin(Vector2 origin, float radius)
    {
        float minTargetDistanceSqr = minTargetDistanceFromCurrent * minTargetDistanceFromCurrent;
        Vector2 currentPosition = enemy.GetPosition2D();

        if (TileNavWorld.Instance == null)
        {
            for (int i = 0; i < maxCandidateChecks; i++)
            {
                Vector2 candidate = origin + Random.insideUnitCircle * radius;
                if ((candidate - currentPosition).sqrMagnitude >= minTargetDistanceSqr)
                    return candidate;
            }

            return origin;
        }

        for (int i = 0; i < maxCandidateChecks; i++)
        {
            Vector2 candidate = origin + Random.insideUnitCircle * radius;

            if ((candidate - currentPosition).sqrMagnitude < minTargetDistanceSqr)
                continue;

            if (TileNavWorld.Instance.IsWalkableWorldPos(candidate) && IsReachable(candidate))
                return candidate;
        }

        return FindNearestWalkablePoint(origin);
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

#if UNITY_EDITOR
    private void OnValidate()
    {
        wanderRadius = Mathf.Max(0.1f, wanderRadius);
        destinationTolerance = Mathf.Max(0.01f, destinationTolerance);
        maxCandidateChecks = Mathf.Max(1, maxCandidateChecks);
        maxCandidatePathRange = Mathf.Max(1, maxCandidatePathRange);
        nearestWalkableSearchRadius = Mathf.Max(1, nearestWalkableSearchRadius);
        minTargetDistanceFromCurrent = Mathf.Max(0f, minTargetDistanceFromCurrent);
        directionLerpSpeed = Mathf.Max(0.1f, directionLerpSpeed);
        minMoveDirectionSqr = Mathf.Max(0.0001f, minMoveDirectionSqr);
    }
#endif
}
