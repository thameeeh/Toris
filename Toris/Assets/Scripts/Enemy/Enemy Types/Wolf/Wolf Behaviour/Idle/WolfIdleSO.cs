using UnityEngine;

[CreateAssetMenu(fileName = "Wolf_Idle_Wander", menuName = "Enemy Logic/Idle Logic/Wolf Idle Wander")]
public class WolfIdleSO : IdleSOBase<Wolf>
{
    [Header("Wander Settings")]
    [SerializeField] private float _wanderRadius = 10f;
    [SerializeField] private float _wanderInterval = 3f;

    [Tooltip("Squared distance threshold to decide we've reached the wander point.")]
    [SerializeField] private float _wanderPointReachSqr = 1.0f;

    [Header("Steering Smoothing")]
    [Tooltip("How quickly the wolf turns towards a new path direction. Higher = snappier.")]
    [SerializeField] private float _directionLerpSpeed = 6f;

    private float _movementSpeed;

    private float _timer;
    private Vector2 _wanderPoint;
    private Vector2 _currentMoveDir;

    private GridPathAgent _pathAgent;

    public override void Initialize(GameObject gameObject, Wolf enemy, Transform player)
    {
        base.Initialize(gameObject, enemy, player);

        _pathAgent = enemy.GetComponent<GridPathAgent>();
        if (_pathAgent == null)
        {
            Debug.LogWarning($"[WolfIdleSO] No GridPathAgent on {enemy.name}. Idle wander will not use pathfinding.");
        }

        _timer = _wanderInterval;
        _wanderPoint = enemy.transform.position;
        _currentMoveDir = Vector2.zero;
    }

    public override void DoEnterLogic()
    {
        base.DoEnterLogic();

        _movementSpeed = enemy.MovementSpeed;
        enemy.animator.SetBool("IsMoving", false);

        _timer = _wanderInterval;
        _wanderPoint = enemy.transform.position;
        _currentMoveDir = Vector2.zero;
    }

    public override void DoExitLogic()
    {
        base.DoExitLogic();

        enemy.animator.SetBool("IsMoving", false);
        enemy.MoveEnemy(Vector2.zero);
        _currentMoveDir = Vector2.zero;
    }

    public override void DoFrameUpdateLogic()
    {
        base.DoFrameUpdateLogic();

        _timer += Time.deltaTime;

        Vector2 currentPos = enemy.transform.position;
        float sqrDistToWander = (_wanderPoint - currentPos).sqrMagnitude;

        if (_timer >= _wanderInterval || sqrDistToWander < _wanderPointReachSqr)
        {
            _wanderPoint = GetRandomWalkableWanderPoint(currentPos);
            _timer = 0f;
        }

        Vector2 desiredDir = Vector2.zero;

        if (_pathAgent != null && TileNavWorld.Instance != null)
        {
            desiredDir = _pathAgent.GetMoveDirection(_wanderPoint);
        }

        if (desiredDir.sqrMagnitude > 0.0001f)
        {
            desiredDir.Normalize();
            _currentMoveDir = Vector2.Lerp(
                _currentMoveDir,
                desiredDir,
                _directionLerpSpeed * Time.deltaTime
            );
        }
        else
        {
            _currentMoveDir = Vector2.Lerp(
                _currentMoveDir,
                Vector2.zero,
                _directionLerpSpeed * Time.deltaTime
            );
        }

        // Apply movement + animation
        if (_currentMoveDir.sqrMagnitude > 0.01f)
        {
            enemy.animator.SetBool("IsMoving", true);
            enemy.MoveEnemy(_currentMoveDir.normalized * _movementSpeed);
            enemy.UpdateAnimationDirection(_currentMoveDir);
        }
        else
        {
            enemy.animator.SetBool("IsMoving", false);
            enemy.MoveEnemy(Vector2.zero);
        }
    }

    public override void DoPhysicsLogic()
    {
        base.DoPhysicsLogic();
    }

    public override void ResetValues()
    {
        base.ResetValues();
    }

    private Vector2 GetRandomWalkableWanderPoint(Vector2 origin)
    {
        const int maxTries = 10;

        if (TileNavWorld.Instance == null)
        {
            return origin + Random.insideUnitCircle * _wanderRadius;
        }

        for (int i = 0; i < maxTries; i++)
        {
            Vector2 offset = Random.insideUnitCircle * _wanderRadius;
            Vector2 candidate = origin + offset;

            if (TileNavWorld.Instance.IsWalkableWorldPos(candidate))
                return candidate;
        }

        return origin;
    }

    public override void DoAnimationTriggerEventLogic(Wolf.AnimationTriggerType triggerType)
    {
        base.DoAnimationTriggerEventLogic(triggerType);
    }
}
