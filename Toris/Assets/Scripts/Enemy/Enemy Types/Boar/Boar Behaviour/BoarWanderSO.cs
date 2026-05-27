using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Boar_Wander_Roam", menuName = "Outland Haven/Enemy/Behaviors/Wander Logic/Boar Wander")]
public class BoarWanderSO : EnemyBehaviourSO<Boar>
{
    [SerializeField, Min(0.1f)] private float wanderRadius = 4f;
    [SerializeField, Min(0.01f)] private float destinationTolerance = 0.25f;
    [SerializeField, Min(1)] private int maxCandidateChecks = 10;
    [SerializeField, Min(1)] private int maxCandidatePathRange = 20;
    [SerializeField, Min(0f)] private float minTargetDistanceFromCurrent = 1f;
    [SerializeField, Min(0.1f)] private float directionLerpSpeed = 6f;
    [SerializeField, Min(0.0001f)] private float minMoveDirectionSqr = 0.0001f;

    [Header("Home")]
    [SerializeField, Range(0.1f, 1f)] private float homeWanderRadiusUsage = 0.7f;

    private readonly List<Vector3> _candidatePath = new List<Vector3>();
    private GridPathAgent _pathAgent;
    private Vector2 _targetPosition;
    private Vector2 _currentMoveDirection;

    public override void Initialize(GameObject gameObject, Boar enemy, Transform player)
    {
        base.Initialize(gameObject, enemy, player);
        enemy.TryGetComponent(out _pathAgent);
    }

    public override void DoEnterLogic()
    {
        base.DoEnterLogic();

        _targetPosition = PickWanderTarget();
        _currentMoveDirection = Vector2.zero;
        enemy.SetMovementAnimation(true);
    }

    public override void DoFrameUpdateLogic()
    {
        base.DoFrameUpdateLogic();

        if (enemy.CanStartleCharge)
        {
            enemy.StateMachine.ChangeState(enemy.ChargeState);
            return;
        }

        float toleranceSqr = destinationTolerance * destinationTolerance;
        if ((_targetPosition - enemy.GetPosition2D()).sqrMagnitude <= toleranceSqr)
            enemy.StateMachine.ChangeState(enemy.IdleState);
    }

    public override void DoPhysicsLogic()
    {
        base.DoPhysicsLogic();

        if (enemy.StateMachine.CurrentEnemyState != enemy.WanderState)
            return;

        Vector2 desiredDirection = Vector2.zero;
        if (_pathAgent != null)
            desiredDirection = _pathAgent.GetMoveDirection(_targetPosition);

        if (desiredDirection.sqrMagnitude <= minMoveDirectionSqr && TileNavWorld.Instance == null)
        {
            Vector2 directDirection = _targetPosition - enemy.GetPosition2D();
            if (directDirection.sqrMagnitude > minMoveDirectionSqr)
                desiredDirection = directDirection.normalized;
        }

        if (desiredDirection.sqrMagnitude > minMoveDirectionSqr)
        {
            _currentMoveDirection = Vector2.Lerp(
                _currentMoveDirection,
                desiredDirection.normalized,
                directionLerpSpeed * Time.fixedDeltaTime);

            enemy.MoveBoar(_currentMoveDirection.normalized, enemy.WanderSpeed);
            return;
        }

        enemy.StateMachine.ChangeState(enemy.IdleState);
    }

    public override void DoExitLogic()
    {
        enemy.StopBoar();
        _currentMoveDirection = Vector2.zero;
        base.DoExitLogic();
    }

    private Vector2 PickWanderTarget()
    {
        Vector2 currentPosition = enemy.GetPosition2D();
        Vector2 origin = ResolveWanderOrigin(currentPosition, out float radius);
        float minTargetDistanceSqr = minTargetDistanceFromCurrent * minTargetDistanceFromCurrent;

        for (int i = 0; i < maxCandidateChecks; i++)
        {
            Vector2 candidate = origin + Random.insideUnitCircle * radius;
            if ((candidate - currentPosition).sqrMagnitude < minTargetDistanceSqr)
                continue;

            if (IsReachable(candidate))
                return candidate;
        }

        return currentPosition;
    }

    private Vector2 ResolveWanderOrigin(Vector2 currentPosition, out float radius)
    {
        radius = wanderRadius;
        if (!enemy.HasHome)
            return currentPosition;

        radius = Mathf.Max(0.1f, enemy.HomeRadius * homeWanderRadiusUsage);

        return (Vector2)enemy.HomeCenter;
    }

    private bool IsReachable(Vector2 worldPosition)
    {
        TileNavWorld nav = TileNavWorld.Instance;
        if (nav == null)
            return true;

        if (!nav.IsWalkableWorldPos(worldPosition) || !nav.IsWalkableWorldPos(enemy.GetPosition2D()))
            return false;

        return TilePathfinder.TryFindPath(
            enemy.GetPosition2D(),
            worldPosition,
            _candidatePath,
            maxCandidatePathRange);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        wanderRadius = Mathf.Max(0.1f, wanderRadius);
        destinationTolerance = Mathf.Max(0.01f, destinationTolerance);
        maxCandidateChecks = Mathf.Max(1, maxCandidateChecks);
        maxCandidatePathRange = Mathf.Max(1, maxCandidatePathRange);
        minTargetDistanceFromCurrent = Mathf.Max(0f, minTargetDistanceFromCurrent);
        directionLerpSpeed = Mathf.Max(0.1f, directionLerpSpeed);
        minMoveDirectionSqr = Mathf.Max(0.0001f, minMoveDirectionSqr);
        homeWanderRadiusUsage = Mathf.Clamp(homeWanderRadiusUsage, 0.1f, 1f);
    }
#endif
}
