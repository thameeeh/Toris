using UnityEngine;

[CreateAssetMenu(fileName = "Necromancer_Idle_Stand", menuName = "Enemy Logic/Idle Logic/Necromancer Idle")]
public class NecromancerIdleSO : IdleSOBase<Necromancer>
{
    [Header("Roaming")]
    [SerializeField] private float roamRadius = 4f;
    [SerializeField] private float arriveDistance = 0.25f;
    [SerializeField] private float retargetDistance = 0.35f;
    [SerializeField] private float idleMoveSpeedMultiplier = 0.65f;

    [Header("Form Timing")]
    [SerializeField] private float minHumanIdleBeforeFloat = 10f;
    [SerializeField] private float maxHumanIdleBeforeFloat = 15f;
    [SerializeField] private float floaterIdleDuration = 10f;

    [Header("Movement")]
    [SerializeField] private float minMoveDirectionSqr = 0.0001f;
    [SerializeField] private int maxWanderTargetAttempts = 8;

    private GridPathAgent _pathAgent;
    private Vector2 _roamCenter;
    private Vector2 _wanderTarget;
    private float _arriveDistanceSqr;
    private float _retargetDistanceSqr;
    private float _humanFloatTimer;
    private float _floaterTimer;
    private bool _isFloaterTimerRunning;

    public override void Initialize(GameObject gameObject, Necromancer enemy, Transform player)
    {
        base.Initialize(gameObject, enemy, player);
        enemy.TryGetComponent(out _pathAgent);
        CacheSquaredThresholds();
    }

    public override void DoEnterLogic()
    {
        base.DoEnterLogic();

        CacheSquaredThresholds();
        _roamCenter = enemy.transform.position;
        _wanderTarget = GetNextWanderTarget();
        _humanFloatTimer = GetNextHumanFloatDelay();
        _floaterTimer = floaterIdleDuration;
        _isFloaterTimerRunning = false;
        enemy.SetMovementAnimation(false);
    }

    public override void DoExitLogic()
    {
        base.DoExitLogic();
        enemy.SetMovementAnimation(false);
        enemy.MoveEnemy(Vector2.zero);
    }

    public override void DoFrameUpdateLogic()
    {
        base.DoFrameUpdateLogic();

        if (UpdateIdleFormTimers())
        {
            StopMoving();
            return;
        }

        UpdateRoaming();
    }

    public override void ResetValues()
    {
        base.ResetValues();

        _roamCenter = enemy != null ? enemy.transform.position : Vector2.zero;
        _wanderTarget = _roamCenter;
        _humanFloatTimer = 0f;
        _floaterTimer = 0f;
        _isFloaterTimerRunning = false;
    }

    private bool UpdateIdleFormTimers()
    {
        if (enemy.IsChangingForm)
            return false;

        if (enemy.IsHumanForm)
        {
            _isFloaterTimerRunning = false;
            _humanFloatTimer -= Time.deltaTime;

            if (_humanFloatTimer <= 0f)
            {
                _humanFloatTimer = GetNextHumanFloatDelay();
                return enemy.RequestBecomeFloater();
            }

            return false;
        }

        if (!enemy.IsFloaterForm)
            return false;

        if (!_isFloaterTimerRunning)
        {
            _floaterTimer = floaterIdleDuration;
            _isFloaterTimerRunning = true;
        }

        _floaterTimer -= Time.deltaTime;
        if (_floaterTimer <= 0f)
        {
            _isFloaterTimerRunning = false;
            return enemy.RequestBecomeHuman();
        }

        return false;
    }

    private void UpdateRoaming()
    {
        if (enemy.IsChangingForm)
        {
            StopMoving();
            return;
        }

        Vector2 currentPosition = enemy.transform.position;
        Vector2 toTarget = _wanderTarget - currentPosition;

        if (toTarget.sqrMagnitude <= _arriveDistanceSqr)
        {
            _wanderTarget = GetNextWanderTarget();
            toTarget = _wanderTarget - currentPosition;
        }

        Vector2 moveDirection = Vector2.zero;
        if (_pathAgent != null)
            moveDirection = _pathAgent.GetMoveDirection(_wanderTarget);

        if (moveDirection.sqrMagnitude <= minMoveDirectionSqr)
            moveDirection = toTarget.sqrMagnitude > _retargetDistanceSqr ? toTarget.normalized : Vector2.zero;

        if (moveDirection.sqrMagnitude <= minMoveDirectionSqr)
        {
            _wanderTarget = GetNextWanderTarget();
            StopMoving();
            return;
        }

        enemy.SetMovementAnimation(enemy.IsHumanForm);
        enemy.MoveEnemy(moveDirection.normalized * enemy.MovementSpeed * idleMoveSpeedMultiplier);
    }

    private Vector2 GetNextWanderTarget()
    {
        float retargetDistanceSqr = _retargetDistanceSqr;
        Vector2 currentPosition = enemy.transform.position;

        for (int attempt = 0; attempt < maxWanderTargetAttempts; attempt++)
        {
            Vector2 candidate = _roamCenter + (Random.insideUnitCircle * roamRadius);
            if ((candidate - currentPosition).sqrMagnitude > retargetDistanceSqr)
                return candidate;
        }

        return _roamCenter;
    }

    private float GetNextHumanFloatDelay()
    {
        float minDelay = Mathf.Min(minHumanIdleBeforeFloat, maxHumanIdleBeforeFloat);
        float maxDelay = Mathf.Max(minHumanIdleBeforeFloat, maxHumanIdleBeforeFloat);
        return Random.Range(minDelay, maxDelay);
    }

    private void CacheSquaredThresholds()
    {
        _arriveDistanceSqr = arriveDistance * arriveDistance;
        _retargetDistanceSqr = retargetDistance * retargetDistance;
    }

    private void StopMoving()
    {
        enemy.SetMovementAnimation(false);
        enemy.MoveEnemy(Vector2.zero);
    }
}
