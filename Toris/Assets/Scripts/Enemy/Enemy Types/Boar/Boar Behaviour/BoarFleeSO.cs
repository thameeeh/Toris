using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Boar_Flee_AfterCharge", menuName = "Enemy Logic/Flee Logic/Boar Flee")]
public class BoarFleeSO : EnemyBehaviourSO<Boar>
{
    [SerializeField, Min(0.1f)] private float minimumFleeDuration = 2f;
    [SerializeField, Min(0.1f)] private float fleeTargetDistance = 6f;
    [SerializeField, Min(0.1f)] private float retargetInterval = 0.75f;
    [SerializeField, Min(0.01f)] private float targetTolerance = 0.3f;
    [SerializeField, Min(1)] private int maxCandidateChecks = 12;
    [SerializeField, Min(1)] private int maxCandidatePathRange = 25;
    [SerializeField, Range(0f, 180f)] private float fleeAngleSpread = 70f;
    [SerializeField, Range(0.1f, 1f)] private float minFleeDistanceMultiplier = 0.6f;
    [SerializeField, Min(0.1f)] private float directionLerpSpeed = 9f;
    [SerializeField, Min(0f)] private float decelerationDuration = 0.75f;
    [SerializeField, Min(0.0001f)] private float minMoveDirectionSqr = 0.0001f;
    [SerializeField, Min(0f)] private float postFleeChargeIgnoreDuration = 1.5f;

    private readonly List<Vector3> _candidatePath = new List<Vector3>();
    private GridPathAgent _pathAgent;
    private Vector2 _fleeTarget;
    private Vector2 _currentMoveDirection;
    private float _fleeUntilTime;
    private float _nextRetargetTime;
    private float _currentSpeed;

    public override void Initialize(GameObject gameObject, Boar enemy, Transform player)
    {
        base.Initialize(gameObject, enemy, player);
        enemy.TryGetComponent(out _pathAgent);
    }

    public override void DoEnterLogic()
    {
        base.DoEnterLogic();

        enemy.IgnoreStartleFor(minimumFleeDuration + postFleeChargeIgnoreDuration);
        _fleeUntilTime = Time.time + minimumFleeDuration;
        _currentMoveDirection = Vector2.zero;
        _currentSpeed = Mathf.Max(enemy.ChargeSpeed, enemy.FleeSpeed);
        _nextRetargetTime = 0f;
        RetargetFlee();
    }

    public override void DoFrameUpdateLogic()
    {
        base.DoFrameUpdateLogic();

        if (Time.time >= _nextRetargetTime)
            RetargetFlee();

        float targetToleranceSqr = targetTolerance * targetTolerance;
        if ((_fleeTarget - enemy.GetPosition2D()).sqrMagnitude <= targetToleranceSqr)
            RetargetFlee();

        if (Time.time >= _fleeUntilTime && !enemy.IsAggroed)
            enemy.StateMachine.ChangeState(enemy.IdleState);
    }

    public override void DoPhysicsLogic()
    {
        base.DoPhysicsLogic();

        if (enemy.StateMachine.CurrentEnemyState != enemy.FleeState)
            return;

        Vector2 desiredDirection = Vector2.zero;
        if (_pathAgent != null)
            desiredDirection = _pathAgent.GetMoveDirection(_fleeTarget);

        if (desiredDirection.sqrMagnitude <= minMoveDirectionSqr && TileNavWorld.Instance == null)
        {
            Vector2 directDirection = _fleeTarget - enemy.GetPosition2D();
            if (directDirection.sqrMagnitude > minMoveDirectionSqr)
                desiredDirection = directDirection.normalized;
        }

        if (desiredDirection.sqrMagnitude > minMoveDirectionSqr)
        {
            _currentMoveDirection = Vector2.Lerp(
                _currentMoveDirection,
                desiredDirection.normalized,
                directionLerpSpeed * Time.fixedDeltaTime);

            UpdateFleeSpeed();
            enemy.MoveBoar(_currentMoveDirection.normalized, _currentSpeed);
            return;
        }

        enemy.StopBoar();
    }

    public override void DoExitLogic()
    {
        enemy.StopBoar();
        _currentMoveDirection = Vector2.zero;
        _currentSpeed = 0f;
        base.DoExitLogic();
    }

    private void RetargetFlee()
    {
        Vector2 currentPosition = enemy.GetPosition2D();
        Vector2 fleeDirection = enemy.TryGetLastChargeDirection(out Vector2 chargeDirection)
            ? chargeDirection
            : Random.insideUnitCircle;

        if (fleeDirection.sqrMagnitude <= minMoveDirectionSqr
            && enemy.TryGetLastThreatPosition(out Vector2 threatPosition))
            fleeDirection = currentPosition - threatPosition;

        if (fleeDirection.sqrMagnitude <= minMoveDirectionSqr)
            fleeDirection = Random.insideUnitCircle;

        if (fleeDirection.sqrMagnitude <= minMoveDirectionSqr)
            fleeDirection = Vector2.down;

        _fleeTarget = PickFleeTarget(currentPosition, fleeDirection.normalized);
        _nextRetargetTime = Time.time + retargetInterval;
    }

    private Vector2 PickFleeTarget(Vector2 currentPosition, Vector2 fleeDirection)
    {
        TileNavWorld nav = TileNavWorld.Instance;
        Vector2 desiredTarget = currentPosition + fleeDirection * fleeTargetDistance;
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
            if (!IsReachable(candidate))
                continue;

            float score = (candidate - currentPosition).sqrMagnitude;
            if (score <= bestScore)
                continue;

            bestScore = score;
            bestCandidate = candidate;
            foundCandidate = true;
        }

        return foundCandidate ? bestCandidate : currentPosition;
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

    private static Vector2 Rotate(Vector2 direction, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);

        return new Vector2(
            direction.x * cos - direction.y * sin,
            direction.x * sin + direction.y * cos);
    }

    private void UpdateFleeSpeed()
    {
        if (decelerationDuration <= 0f)
        {
            _currentSpeed = enemy.FleeSpeed;
            return;
        }

        float speedDelta = Mathf.Abs(enemy.ChargeSpeed - enemy.FleeSpeed);
        float decelerationRate = speedDelta / decelerationDuration;
        _currentSpeed = Mathf.MoveTowards(
            _currentSpeed,
            enemy.FleeSpeed,
            decelerationRate * Time.fixedDeltaTime);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        minimumFleeDuration = Mathf.Max(0.1f, minimumFleeDuration);
        fleeTargetDistance = Mathf.Max(0.1f, fleeTargetDistance);
        retargetInterval = Mathf.Max(0.1f, retargetInterval);
        targetTolerance = Mathf.Max(0.01f, targetTolerance);
        maxCandidateChecks = Mathf.Max(1, maxCandidateChecks);
        maxCandidatePathRange = Mathf.Max(1, maxCandidatePathRange);
        minFleeDistanceMultiplier = Mathf.Clamp(minFleeDistanceMultiplier, 0.1f, 1f);
        directionLerpSpeed = Mathf.Max(0.1f, directionLerpSpeed);
        decelerationDuration = Mathf.Max(0f, decelerationDuration);
        minMoveDirectionSqr = Mathf.Max(0.0001f, minMoveDirectionSqr);
        postFleeChargeIgnoreDuration = Mathf.Max(0f, postFleeChargeIgnoreDuration);
    }
#endif
}
