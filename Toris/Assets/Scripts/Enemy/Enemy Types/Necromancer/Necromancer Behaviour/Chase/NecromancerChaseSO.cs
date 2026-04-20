using UnityEngine;

[CreateAssetMenu(fileName = "Necromancer_Chase_CastRange", menuName = "Enemy Logic/Chase Logic/Necromancer Cast Range Chase")]
public class NecromancerChaseSO : ChaseSOBase<Necromancer>
{
    [Header("Ranges")]
    [SerializeField] private float preferredDistance = 6f;
    [SerializeField] private float retreatDistance = 4f;
    [SerializeField, Min(0f)] private float preferredDistanceInsideCastingRangePadding = 0.35f;

    [Header("Form Timing")]
    [SerializeField, Min(0f)] private float humanToFloaterTriggerDelay = 0.2f;

    [Header("Pathing")]
    [SerializeField] private float minMoveDirectionSqr = 0.0001f;
    [SerializeField] private float floaterMoveSpeedMultiplier = 0.8f;

    [Header("Post Cast Reposition")]
    [SerializeField] private float postCastRepositionDuration = 0.75f;
#if UNITY_EDITOR
    [Header("Debug")]
    [SerializeField] private bool debugChaseDecisions;
#endif

    private GridPathAgent _pathAgent;
    private CircleCollider2D _castingRangeCollider;
    private float _preferredDistanceSqr;
    private float _retreatDistanceSqr;
    private float _postCastRepositionTimer;
    private float _humanToFloaterDelayTimer;
    private bool _isPostCastRepositioning;
    private bool _isWaitingForHumanToFloaterDelay;
#if UNITY_EDITOR
    private const float DebugChaseLogInterval = 0.1f;
    private string _lastChaseDecision = string.Empty;
    private float _nextChaseDebugLogTime;
#endif

    public bool CanStartSelectedAttack { get; private set; }
    public NecromancerAttackType SelectedAttackType { get; private set; }
    public bool CanCastSpell => CanStartSelectedAttack && SelectedAttackType == NecromancerAttackType.SpellCast;
    public bool IsInPanicRange { get; private set; }
    public bool IsInCastingRange { get; private set; }

    public override void Initialize(GameObject gameObject, Necromancer enemy, Transform player)
    {
        base.Initialize(gameObject, enemy, player);
        enemy.TryGetComponent(out _pathAgent);
        CacheCastingRangeCollider();
        CacheSquaredRanges();
    }

    public override void DoEnterLogic()
    {
        base.DoEnterLogic();

        CacheSquaredRanges();
        ResetAttackSelection();
        IsInPanicRange = false;
        IsInCastingRange = false;
        _postCastRepositionTimer = enemy.RequiresPostCastReposition ? postCastRepositionDuration : 0f;
        _isPostCastRepositioning = enemy.RequiresPostCastReposition;
        ResetHumanToFloaterDelay();
    }

    public override void DoExitLogic()
    {
        base.DoExitLogic();

        ResetAttackSelection();
        IsInPanicRange = false;
        IsInCastingRange = false;
        ResetHumanToFloaterDelay();
        enemy.SetMovementAnimation(false);
        enemy.MoveEnemy(Vector2.zero);
    }

    public override void DoFrameUpdateLogic()
    {
        base.DoFrameUpdateLogic();

        ResetAttackSelection();
        IsInPanicRange = false;
        IsInCastingRange = false;

        if (playerTransform == null)
        {
            ResetHumanToFloaterDelay();
            StopMoving();
            LogChaseDecision("NoPlayer", 0f, enemy.IsWithinCastingRange, enemy.IsWithinStrikingDistance);
            return;
        }

        Vector2 enemyPosition = enemy.transform.position;
        Vector2 playerPosition = playerTransform.position;
        Vector2 toPlayer = playerPosition - enemyPosition;
        float distanceToPlayerSqr = toPlayer.sqrMagnitude;

        if (enemy.IsChangingForm)
        {
            ResetHumanToFloaterDelay();
            StopMoving();
            enemy.FacePlayer();
            LogChaseDecision("ChangingForm", distanceToPlayerSqr, enemy.IsWithinCastingRange, enemy.IsWithinStrikingDistance);
            return;
        }

        EnsurePostCastRepositionStarted();

        IsInCastingRange = enemy.IsWithinCastingRange;
        if (TryStartCastingRangeAttack())
            return;

        if (enemy.IsWithinStrikingDistance)
        {
            IsInPanicRange = true;

            if (enemy.IsHumanForm)
            {
                StopMoving();
                enemy.FacePlayer();
                LogChaseDecision("PanicRangeHumanDelay", distanceToPlayerSqr, IsInCastingRange, true);
                TryRequestHumanToFloater();
                return;
            }

            if (enemy.IsReadyToCastAnimation && enemy.CanStartAttack(NecromancerAttackType.PanicSwing))
            {
                StopMoving();
                enemy.FacePlayer();
                ResetHumanToFloaterDelay();
                SelectAttack(NecromancerAttackType.PanicSwing);
                LogChaseDecision("PanicSwingReady", distanceToPlayerSqr, IsInCastingRange, true);
                return;
            }

            ResetHumanToFloaterDelay();
            LogChaseDecision("PanicRetreat", distanceToPlayerSqr, IsInCastingRange, true);
            MoveAwayFromPlayer(toPlayer);
            AdvancePostCastReposition(distanceToPlayerSqr);
            return;
        }

        if (enemy.ShouldPrioritizePostAttackReposition && UpdatePostCastReposition(toPlayer, distanceToPlayerSqr))
        {
            ResetHumanToFloaterDelay();
            LogChaseDecision("PostCastRepositionPriority", distanceToPlayerSqr, IsInCastingRange, false);
            return;
        }

        if (UpdatePostCastReposition(toPlayer, distanceToPlayerSqr))
        {
            ResetHumanToFloaterDelay();
            LogChaseDecision("PostCastReposition", distanceToPlayerSqr, IsInCastingRange, false);
            return;
        }

        if (distanceToPlayerSqr < _retreatDistanceSqr)
        {
            if (enemy.IsHumanForm)
            {
                StopMoving();
                enemy.FacePlayer();
                LogChaseDecision("RetreatBandHumanDelay", distanceToPlayerSqr, IsInCastingRange, false);
                TryRequestHumanToFloater();
                return;
            }

            ResetHumanToFloaterDelay();
            LogChaseDecision("RetreatBandMoveAway", distanceToPlayerSqr, IsInCastingRange, false);
            MoveAwayFromPlayer(toPlayer);
            return;
        }

        if (IsInCastingRange)
        {
            StopMoving();
            enemy.FacePlayer();

            if (enemy.IsHumanForm)
            {
                LogChaseDecision("CastingRangeHumanDelay", distanceToPlayerSqr, true, false);
                TryRequestHumanToFloater();
                return;
            }

            ResetHumanToFloaterDelay();
            SelectAttack(enemy.IsPhaseTwoSummonUnlocked && enemy.CanStartAttack(NecromancerAttackType.Summon)
                ? NecromancerAttackType.Summon
                : NecromancerAttackType.SpellCast);
            LogChaseDecision(
                CanStartSelectedAttack ? $"CastingRangeAttackReady:{SelectedAttackType}" : $"CastingRangeHold:{SelectedAttackType}",
                distanceToPlayerSqr,
                true,
                false);
            return;
        }

        ResetHumanToFloaterDelay();
        LogChaseDecision("ApproachPlayer", distanceToPlayerSqr, false, false);
        MoveTowardPlayer(playerPosition, toPlayer);
    }

    public override void ResetValues()
    {
        base.ResetValues();
        ResetAttackSelection();
        IsInPanicRange = false;
        IsInCastingRange = false;
        _postCastRepositionTimer = 0f;
        _isPostCastRepositioning = false;
        ResetHumanToFloaterDelay();
    }

    private void StopMoving()
    {
        enemy.SetMovementAnimation(false);
        enemy.MoveEnemy(Vector2.zero);
    }

    private void MoveTowardPlayer(Vector2 playerPosition, Vector2 toPlayer)
    {
        Vector2 moveDirection = Vector2.zero;
        if (_pathAgent != null)
            moveDirection = _pathAgent.GetMoveDirection(playerPosition);

        if (moveDirection.sqrMagnitude <= minMoveDirectionSqr)
            moveDirection = toPlayer.sqrMagnitude > minMoveDirectionSqr ? toPlayer.normalized : Vector2.zero;

        MoveInDirection(moveDirection);
    }

    private void MoveAwayFromPlayer(Vector2 toPlayer)
    {
        Vector2 moveDirection = toPlayer.sqrMagnitude > minMoveDirectionSqr ? -toPlayer.normalized : Vector2.zero;
        MoveInDirection(moveDirection);
    }

    private void MoveInDirection(Vector2 moveDirection)
    {
        if (moveDirection.sqrMagnitude <= minMoveDirectionSqr)
        {
            StopMoving();
            return;
        }

        enemy.SetMovementAnimation(enemy.IsHumanForm);
        enemy.MoveEnemy(moveDirection.normalized * GetCurrentMoveSpeed());
    }

    private bool UpdatePostCastReposition(Vector2 toPlayer, float distanceToPlayerSqr)
    {
        EnsurePostCastRepositionStarted();

        if (!_isPostCastRepositioning)
            return false;

        MoveAwayFromPlayer(toPlayer);
        AdvancePostCastReposition(distanceToPlayerSqr);

        return true;
    }

    private float GetCurrentMoveSpeed()
    {
        float moveSpeed;

        if (enemy.IsFloaterForm || enemy.IsReadyToCastAnimation)
            moveSpeed = enemy.MovementSpeed * floaterMoveSpeedMultiplier;
        else
            moveSpeed = enemy.MovementSpeed;

        if (_isPostCastRepositioning)
            moveSpeed *= enemy.PostAttackRepositionSpeedMultiplier;

        return moveSpeed;
    }

    private void CacheSquaredRanges()
    {
        float resolvedPreferredDistance = preferredDistance;
        if (_castingRangeCollider != null)
        {
            float colliderScale = Mathf.Max(
                Mathf.Abs(_castingRangeCollider.transform.lossyScale.x),
                Mathf.Abs(_castingRangeCollider.transform.lossyScale.y));
            float castingRangeRadius = _castingRangeCollider.radius * colliderScale;
            float maxPreferredDistanceInsideCastingRange = Mathf.Max(
                retreatDistance,
                castingRangeRadius - preferredDistanceInsideCastingRangePadding);
            resolvedPreferredDistance = Mathf.Min(resolvedPreferredDistance, maxPreferredDistanceInsideCastingRange);
        }

        _preferredDistanceSqr = resolvedPreferredDistance * resolvedPreferredDistance;
        _retreatDistanceSqr = retreatDistance * retreatDistance;
    }

    private void CacheCastingRangeCollider()
    {
        if (_castingRangeCollider != null || enemy == null)
            return;

        NecromancerCastingRangeCheck castingRangeCheck = enemy.GetComponentInChildren<NecromancerCastingRangeCheck>(true);
        if (castingRangeCheck != null)
            castingRangeCheck.TryGetComponent(out _castingRangeCollider);
    }

#if UNITY_EDITOR
    private void LogChaseDecision(string decision, float distanceToPlayerSqr, bool inCastingRange, bool inPanicRange)
    {
        if (!debugChaseDecisions || enemy == null)
            return;

        string message =
            $"decision={decision} " +
            $"dist={Mathf.Sqrt(distanceToPlayerSqr):0.##} " +
            $"cast={inCastingRange} " +
            $"panic={inPanicRange} " +
            $"human={enemy.IsHumanForm} " +
            $"floaterReady={enemy.IsReadyToCastAnimation} " +
            $"postCast={_isPostCastRepositioning}";

        float now = Time.time;
        if (_lastChaseDecision == message && now < _nextChaseDebugLogTime)
            return;

        _lastChaseDecision = message;
        _nextChaseDebugLogTime = now + DebugChaseLogInterval;
        Debug.Log($"[NecroChase:{enemy.name}] {message}", enemy);
    }
#endif

    private void EnsurePostCastRepositionStarted()
    {
        if (_isPostCastRepositioning || !enemy.RequiresPostCastReposition)
            return;

        _isPostCastRepositioning = true;
        _postCastRepositionTimer = postCastRepositionDuration;
    }

    private void AdvancePostCastReposition(float distanceToPlayerSqr)
    {
        if (!_isPostCastRepositioning)
            return;

        _postCastRepositionTimer -= Time.deltaTime;

        if (_postCastRepositionTimer <= 0f
            && (distanceToPlayerSqr >= _preferredDistanceSqr || !enemy.IsWithinCastingRange))
        {
            _isPostCastRepositioning = false;
            enemy.ClearPostCastReposition();
        }
    }

    private void TryRequestHumanToFloater()
    {
        if (!enemy.IsHumanForm)
        {
            ResetHumanToFloaterDelay();
            return;
        }

        if (humanToFloaterTriggerDelay <= 0f)
        {
            ResetHumanToFloaterDelay();
            enemy.RequestBecomeFloater();
            return;
        }

        if (!_isWaitingForHumanToFloaterDelay)
        {
            _isWaitingForHumanToFloaterDelay = true;
            _humanToFloaterDelayTimer = humanToFloaterTriggerDelay;

#if UNITY_EDITOR
            enemy.DebugAnimationLog(
                $"Combat human-to-floater delay started ({humanToFloaterTriggerDelay:0.##}s).");
#endif
            return;
        }

        _humanToFloaterDelayTimer -= Time.deltaTime;
        if (_humanToFloaterDelayTimer > 0f)
        {
#if UNITY_EDITOR
            enemy.DebugAnimationDecision(
                $"Waiting {_humanToFloaterDelayTimer:0.##}s before triggering BecomeFloater.");
#endif
            return;
        }

        ResetHumanToFloaterDelay();
        enemy.RequestBecomeFloater();
    }

    private void SelectAttack(NecromancerAttackType attackType)
    {
        SelectedAttackType = attackType;
        CanStartSelectedAttack = enemy.IsReadyToCastAnimation && enemy.CanStartAttack(attackType);
    }

    private bool TryStartCastingRangeAttack()
    {
        if (!enemy.IsWithinCastingRange)
            return false;

        if (enemy.IsHumanForm)
        {
            StopMoving();
            enemy.FacePlayer();
            TryRequestHumanToFloater();
            return true;
        }

        NecromancerAttackType rangedAttackType = GetPreferredCastingRangeAttackType();
        if (!enemy.IsReadyToCastAnimation || !enemy.CanStartAttack(rangedAttackType))
            return false;

        ResetHumanToFloaterDelay();
        StopMoving();
        enemy.FacePlayer();
        SelectAttack(rangedAttackType);
        return true;
    }

    private NecromancerAttackType GetPreferredCastingRangeAttackType()
    {
        return enemy.IsPhaseTwoSummonUnlocked && enemy.CanStartAttack(NecromancerAttackType.Summon)
            ? NecromancerAttackType.Summon
            : NecromancerAttackType.SpellCast;
    }

    private void ResetAttackSelection()
    {
        SelectedAttackType = NecromancerAttackType.SpellCast;
        CanStartSelectedAttack = false;
    }

    private void ResetHumanToFloaterDelay()
    {
        _isWaitingForHumanToFloaterDelay = false;
        _humanToFloaterDelayTimer = 0f;
    }
}
