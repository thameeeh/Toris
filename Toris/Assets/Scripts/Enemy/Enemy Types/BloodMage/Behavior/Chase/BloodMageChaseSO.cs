using UnityEngine;

[CreateAssetMenu(fileName = "BloodMage_Chase_Leashed", menuName = "Enemy Logic/Chase Logic/BloodMage Leashed Chase")]
public class BloodMageChaseSO : ChaseSOBase<BloodMage>
{
    private const float DefaultGuardRadius = 1.75f;
    private const float DefaultGuardPositionTolerance = 0.25f;
    private const float DefaultGuardPositionHysteresis = 0.2f;

    [Header("Ranges")]
    [SerializeField, Min(0f)] private float retreatDistance = 1.5f;
    [SerializeField, Min(0f)] private float leashRadius = 5f;
    [SerializeField, Min(0f)] private float leashHysteresis = 0.5f;
    [SerializeField, Min(0f)] private float guardRadius = 1.75f;
    [SerializeField, Min(0f)] private float guardPositionTolerance = 0.25f;
    [SerializeField, Min(0f)] private float guardPositionHysteresis = 0.2f;
    [SerializeField] private float guardAnchorStartAngleDegrees = 90f;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float kiteMoveSpeedMultiplier = 0.8f;
    [SerializeField, Min(0f)] private float guardMoveSpeedMultiplier = 0.7f;
    [SerializeField, Min(0f)] private float leashReturnSpeedMultiplier = 1.1f;
    [SerializeField, Min(0f)] private float minimumMoveDirectionSqr = 0.0001f;

    private GridPathAgent _pathAgent;
    private float _retreatDistanceSqr;
    private float _leashRadiusSqr;
    private float _leashReturnReleaseDistanceSqr;
    private float _effectiveGuardRadius;
    private float _guardPositionToleranceSqr;
    private float _guardPositionMoveDistanceSqr;
    private bool _isReturningToOwner;
    private bool _isMovingToGuardAnchor;

    public bool CanStartAttack { get; private set; }

    public override void Initialize(GameObject gameObject, BloodMage enemy, Transform player)
    {
        base.Initialize(gameObject, enemy, player);
        enemy.TryGetComponent(out _pathAgent);
        CacheSquaredRanges();
    }

    public override void DoEnterLogic()
    {
        base.DoEnterLogic();
        CacheSquaredRanges();
        CanStartAttack = false;
        _isReturningToOwner = false;
        _isMovingToGuardAnchor = false;
    }

    public override void DoExitLogic()
    {
        base.DoExitLogic();
        CanStartAttack = false;
        _isReturningToOwner = false;
        _isMovingToGuardAnchor = false;
        enemy.MoveEnemy(Vector2.zero);
        enemy.SetMovementAnimation(false);
    }

    public override void DoFrameUpdateLogic()
    {
        base.DoFrameUpdateLogic();
        CanStartAttack = false;

        if (!enemy.HasCombatContext)
        {
            LogDecision("No combat context -> StopMoving");
            StopMoving();
            return;
        }

        Vector2 enemyPosition = enemy.transform.position;
        Vector2 ownerPosition = enemy.Owner.transform.position;
        Vector2 playerPosition = playerTransform.position;
        Vector2 toPlayer = playerPosition - enemyPosition;
        Vector2 toOwner = ownerPosition - enemyPosition;
        float playerDistanceSqr = toPlayer.sqrMagnitude;
        float ownerDistanceSqr = toOwner.sqrMagnitude;

        if (!enemy.ShouldAttackOnOwnerCommand)
        {
            MaintainGuardPosition(enemyPosition);
            return;
        }

        if (_isReturningToOwner)
        {
            if (ownerDistanceSqr > _leashReturnReleaseDistanceSqr)
            {
                LogDecision($"Leash return commit -> ReturnToOwner ownerDist={Mathf.Sqrt(ownerDistanceSqr):0.##}");
                MoveInDirection(GetTowardPositionDirection(ownerPosition, toOwner), leashReturnSpeedMultiplier);
                return;
            }

            _isReturningToOwner = false;
            LogDecision($"Leash return released -> ResumeCombat ownerDist={Mathf.Sqrt(ownerDistanceSqr):0.##}");
        }

        if (ownerDistanceSqr > _leashRadiusSqr)
        {
            _isReturningToOwner = true;
            LogDecision($"Outside leash -> ReturnToOwner ownerDist={Mathf.Sqrt(ownerDistanceSqr):0.##}");
            MoveInDirection(GetTowardPositionDirection(ownerPosition, toOwner), leashReturnSpeedMultiplier);
            return;
        }

        if (enemy.IsWithinAttackRange)
        {
            if (playerDistanceSqr < _retreatDistanceSqr)
            {
                LogDecision($"Player too close -> KiteRetreat playerDist={Mathf.Sqrt(playerDistanceSqr):0.##}");
                MoveInDirection(GetSafeRetreatDirection(toPlayer, toOwner), kiteMoveSpeedMultiplier);
                return;
            }

            LogDecision(
                $"Within attack range -> HoldAndFace playerDist={Mathf.Sqrt(playerDistanceSqr):0.##} canStartAttack={enemy.CanStartAttack}");
            StopMoving();
            enemy.FacePlayer();
            CanStartAttack = enemy.CanStartAttack;
            return;
        }

        LogDecision($"Outside attack range -> ChasePlayer playerDist={Mathf.Sqrt(playerDistanceSqr):0.##}");
        MoveInDirection(GetTowardPositionDirection(playerPosition, toPlayer), 1f);
    }

    public override void ResetValues()
    {
        base.ResetValues();
        CanStartAttack = false;
    }

    public void ResetRuntimeState()
    {
        ResetValues();
    }

    private void StopMoving()
    {
        enemy.SetMovementAnimation(false);
        enemy.MoveEnemy(Vector2.zero);
    }

    private void MaintainGuardPosition(Vector2 enemyPosition)
    {
        Vector2 guardAnchorPosition = enemy.GetGuardAnchorPosition(_effectiveGuardRadius, guardAnchorStartAngleDegrees);
        Vector2 toGuardAnchor = guardAnchorPosition - enemyPosition;
        float guardDistanceSqr = toGuardAnchor.sqrMagnitude;

        if (_isMovingToGuardAnchor)
        {
            if (guardDistanceSqr <= _guardPositionToleranceSqr)
            {
                _isMovingToGuardAnchor = false;
                LogDecision($"Owner idle command -> HoldGuardPosition dist={Mathf.Sqrt(guardDistanceSqr):0.##}");
                StopMoving();
                return;
            }

            LogDecision($"Owner idle command -> GuardAnchorCommit dist={Mathf.Sqrt(guardDistanceSqr):0.##}");
            MoveInDirection(GetTowardPositionDirection(guardAnchorPosition, toGuardAnchor), guardMoveSpeedMultiplier);
            return;
        }

        if (guardDistanceSqr <= _guardPositionMoveDistanceSqr)
        {
            LogDecision($"Owner idle command -> HoldGuardPosition dist={Mathf.Sqrt(guardDistanceSqr):0.##}");
            StopMoving();
            return;
        }

        _isMovingToGuardAnchor = true;
        LogDecision($"Owner idle command -> GuardAnchor dist={Mathf.Sqrt(guardDistanceSqr):0.##}");
        MoveInDirection(GetTowardPositionDirection(guardAnchorPosition, toGuardAnchor), guardMoveSpeedMultiplier);
    }

    private void MoveInDirection(Vector2 moveDirection, float speedMultiplier)
    {
        if (moveDirection.sqrMagnitude <= minimumMoveDirectionSqr)
        {
            LogDecision("MoveInDirection received near-zero vector -> StopMoving");
            StopMoving();
            return;
        }

        LogDecision(
            $"MoveInDirection raw=({moveDirection.x:0.###}, {moveDirection.y:0.###}) speedMult={speedMultiplier:0.##}");
        enemy.SetMovementAnimation(true);
        enemy.MoveEnemy(moveDirection.normalized * enemy.MovementSpeed * speedMultiplier);
    }

    private Vector2 GetTowardPositionDirection(Vector2 targetPosition, Vector2 fallbackDirection)
    {
        Vector2 moveDirection = Vector2.zero;

        if (_pathAgent != null)
            moveDirection = _pathAgent.GetMoveDirection(targetPosition);

        if (moveDirection.sqrMagnitude <= minimumMoveDirectionSqr)
            moveDirection = fallbackDirection.sqrMagnitude > minimumMoveDirectionSqr ? fallbackDirection.normalized : Vector2.zero;

        return moveDirection;
    }

    private Vector2 GetSafeRetreatDirection(Vector2 toPlayer, Vector2 toOwner)
    {
        Vector2 retreatDirection = toPlayer.sqrMagnitude > minimumMoveDirectionSqr ? -toPlayer.normalized : Vector2.zero;
        if (retreatDirection.sqrMagnitude <= minimumMoveDirectionSqr)
            return retreatDirection;

        Vector2 projectedPosition = (Vector2)enemy.transform.position + retreatDirection;
        Vector2 projectedToOwner = (Vector2)enemy.Owner.transform.position - projectedPosition;
        if (projectedToOwner.sqrMagnitude <= _leashRadiusSqr)
            return retreatDirection;

        return toOwner.sqrMagnitude > minimumMoveDirectionSqr ? toOwner.normalized : Vector2.zero;
    }

    private void CacheSquaredRanges()
    {
        _retreatDistanceSqr = retreatDistance * retreatDistance;
        _leashRadiusSqr = leashRadius * leashRadius;
        float leashReturnReleaseDistance = Mathf.Max(0f, leashRadius - leashHysteresis);
        _leashReturnReleaseDistanceSqr = leashReturnReleaseDistance * leashReturnReleaseDistance;
        _effectiveGuardRadius = guardRadius > 0f ? guardRadius : DefaultGuardRadius;
        float effectiveGuardTolerance = guardPositionTolerance > 0f
            ? guardPositionTolerance
            : DefaultGuardPositionTolerance;
        float effectiveGuardHysteresis = guardPositionHysteresis > 0f
            ? guardPositionHysteresis
            : DefaultGuardPositionHysteresis;
        _guardPositionToleranceSqr = effectiveGuardTolerance * effectiveGuardTolerance;
        float guardPositionMoveDistance = effectiveGuardTolerance + effectiveGuardHysteresis;
        _guardPositionMoveDistanceSqr = guardPositionMoveDistance * guardPositionMoveDistance;
    }

    private void LogDecision(string message)
    {
#if UNITY_EDITOR
        enemy.DebugMovementDecision(message);
#endif
    }
}
