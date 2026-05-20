using UnityEngine;

[CreateAssetMenu(fileName = "Boar_Charge_Run", menuName = "Enemy Logic/Attack Logic/Boar Charge")]
public class BoarChargeSO : AttackSOBase<Boar>
{
    [SerializeField, Min(0f)] private float aimDuration = 0.5f;
    [SerializeField, Min(0.1f)] private float chargeDuration = 2.5f;
    [SerializeField, Min(0f)] private float runThroughDistance = 2.5f;
    [SerializeField, Min(0.01f)] private float chargeTargetTolerance = 0.25f;
    [SerializeField, Min(0.0001f)] private float minChargeDirectionSqr = 0.0001f;
    [SerializeField, Min(0f)] private float chargeCooldown = 2.25f;
    [SerializeField] private bool stopOnBlockedNavigation = true;
    [SerializeField, Min(0.01f)] private float blockedNavigationProbeDistance = 0.45f;
    [SerializeField, Min(0f)] private float blockedNavigationSideProbeOffset = 0.25f;

    private Vector2 _chargeDirection;
    private Vector2 _chargeTarget;
    private float _aimTimer;
    private float _chargeTimer;
    private float _nextAllowedChargeTime;
    private bool _isAiming;
    private bool _hasAppliedHit;

    public bool IsComplete { get; private set; }
    public bool CanStartCharge => Time.time >= _nextAllowedChargeTime;

    public void ResetCooldown()
    {
        _nextAllowedChargeTime = 0f;
    }

    public override void DoEnterLogic()
    {
        base.DoEnterLogic();

        enemy.RememberCurrentThreatPosition();

        IsComplete = false;
        _isAiming = aimDuration > 0f;
        _hasAppliedHit = false;
        _aimTimer = aimDuration;
        _chargeTimer = chargeDuration;
        _nextAllowedChargeTime = Time.time + chargeCooldown;

        _chargeDirection = enemy.GetDirectionToAggroTarget();
        if (_chargeDirection.sqrMagnitude <= minChargeDirectionSqr)
            _chargeDirection = (Vector2)enemy.transform.right;

        if (_isAiming)
            enemy.SetMovementAnimation(false, _chargeDirection);
        else
            BeginCharge();
    }

    public override void DoExitLogic()
    {
        base.DoExitLogic();
        enemy.StopBoar();
    }

    public override void DoFrameUpdateLogic()
    {
        base.DoFrameUpdateLogic();

        if (_isAiming)
        {
            UpdateAimDirection();
            _aimTimer -= Time.deltaTime;

            if (_aimTimer > 0f)
                return;

            BeginCharge();
        }

        _chargeTimer -= Time.deltaTime;

        if (!_hasAppliedHit && enemy.IsWithinStrikingDistance)
        {
            _hasAppliedHit = true;
            enemy.DamageCurrentTarget(enemy.ChargeDamage, _chargeDirection);
        }

        if (_chargeTimer <= 0f)
            IsComplete = true;

        float toleranceSqr = chargeTargetTolerance * chargeTargetTolerance;
        if ((_chargeTarget - enemy.GetPosition2D()).sqrMagnitude <= toleranceSqr)
            IsComplete = true;
    }

    public override void DoPhysicsLogic()
    {
        base.DoPhysicsLogic();

        if (IsComplete || _isAiming)
        {
            enemy.StopBoar();
            return;
        }

        if (IsChargePathBlocked())
        {
            IsComplete = true;
            enemy.StopBoar();
            return;
        }

        enemy.MoveBoar(_chargeDirection, enemy.ChargeSpeed);
    }

    public override void ResetValues()
    {
        base.ResetValues();

        IsComplete = false;
        _isAiming = false;
        _chargeDirection = Vector2.zero;
        _chargeTarget = Vector2.zero;
        _aimTimer = 0f;
        _chargeTimer = 0f;
        _hasAppliedHit = false;
    }

    private void UpdateAimDirection()
    {
        enemy.RememberCurrentThreatPosition();

        Vector2 aimDirection = enemy.GetDirectionToAggroTarget();
        if (aimDirection.sqrMagnitude <= minChargeDirectionSqr)
            aimDirection = _chargeDirection;

        if (aimDirection.sqrMagnitude <= minChargeDirectionSqr)
            return;

        _chargeDirection = aimDirection.normalized;
        enemy.SetMovementAnimation(false, _chargeDirection);
    }

    private void BeginCharge()
    {
        _isAiming = false;
        _chargeTimer = chargeDuration;
        _chargeDirection = ResolveChargeDirection();
        _chargeTarget = ResolveChargeTarget(_chargeDirection);

        enemy.RememberChargeDirection(_chargeDirection);
        enemy.SetMovementAnimation(true, _chargeDirection);
    }

    private Vector2 ResolveChargeDirection()
    {
        Vector2 currentPosition = enemy.GetPosition2D();

        if (enemy.TryGetAggroTargetPosition(out Vector2 targetPosition))
        {
            Vector2 directionToTarget = targetPosition - currentPosition;
            if (directionToTarget.sqrMagnitude > minChargeDirectionSqr)
                return directionToTarget.normalized;
        }

        if (enemy.TryGetLastThreatPosition(out Vector2 lastThreatPosition))
        {
            Vector2 directionToThreat = lastThreatPosition - currentPosition;
            if (directionToThreat.sqrMagnitude > minChargeDirectionSqr)
                return directionToThreat.normalized;
        }

        if (_chargeDirection.sqrMagnitude > minChargeDirectionSqr)
            return _chargeDirection.normalized;

        return ((Vector2)enemy.transform.right).normalized;
    }

    private Vector2 ResolveChargeTarget(Vector2 chargeDirection)
    {
        Vector2 currentPosition = enemy.GetPosition2D();
        if (enemy.TryGetAggroTargetPosition(out Vector2 targetPosition))
            return targetPosition + chargeDirection * runThroughDistance;

        if (enemy.TryGetLastThreatPosition(out Vector2 lastThreatPosition))
            return lastThreatPosition + chargeDirection * runThroughDistance;

        return currentPosition + chargeDirection * runThroughDistance;
    }

    private bool IsChargePathBlocked()
    {
        if (!stopOnBlockedNavigation)
            return false;

        TileNavWorld nav = TileNavWorld.Instance;
        if (nav == null)
            return false;

        if (_chargeDirection.sqrMagnitude <= minChargeDirectionSqr)
            return false;

        Vector2 chargeDirection = _chargeDirection.normalized;
        float probeDistance = Mathf.Max(
            blockedNavigationProbeDistance,
            enemy.ChargeSpeed * Time.fixedDeltaTime);
        Vector2 probePosition = enemy.GetPosition2D() + chargeDirection * probeDistance;

        if (!nav.IsWalkableWorldPos(probePosition))
            return true;

        if (blockedNavigationSideProbeOffset <= 0f)
            return false;

        Vector2 sideOffset = new Vector2(-chargeDirection.y, chargeDirection.x)
            * blockedNavigationSideProbeOffset;

        return !nav.IsWalkableWorldPos(probePosition + sideOffset)
               || !nav.IsWalkableWorldPos(probePosition - sideOffset);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        aimDuration = Mathf.Max(0f, aimDuration);
        chargeDuration = Mathf.Max(0.1f, chargeDuration);
        runThroughDistance = Mathf.Max(0f, runThroughDistance);
        chargeTargetTolerance = Mathf.Max(0.01f, chargeTargetTolerance);
        minChargeDirectionSqr = Mathf.Max(0.0001f, minChargeDirectionSqr);
        chargeCooldown = Mathf.Max(0f, chargeCooldown);
        blockedNavigationProbeDistance = Mathf.Max(0.01f, blockedNavigationProbeDistance);
        blockedNavigationSideProbeOffset = Mathf.Max(0f, blockedNavigationSideProbeOffset);
    }
#endif
}
