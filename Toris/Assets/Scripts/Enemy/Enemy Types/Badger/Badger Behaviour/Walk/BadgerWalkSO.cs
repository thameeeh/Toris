using UnityEngine;

[CreateAssetMenu(fileName = "Badger_Walk", menuName = "Enemy Logic/Walk Logic/Badger Walk")]
public class BadgerWalkSO : WalkSOBase<Badger>
{
    [SerializeField] private float WanderRadius = 5f;
    [SerializeField] private float WanderTimer = 2f;

    [Tooltip("How quickly the badger turns towards the path direction.")]
    [SerializeField] private float DirectionLerpSpeed = 6f;

    private float _distance;
    private float _timer;

    private Vector2 _wanderPoint;
    private Vector2 _currentDirection;

    private GridPathAgent _pathAgent;

    public override void Initialize(GameObject gameObject, Badger enemy, Transform player)
    {
        base.Initialize(gameObject, enemy, player);

        _pathAgent = enemy.GetComponent<GridPathAgent>();
        if (_pathAgent == null)
        {
            Debug.LogWarning($"[BadgerWalkSO] No GridPathAgent on {enemy.name}. " +
                             "Badger walk will not use tile pathfinding.");
        }

        _timer = 0f;
        _wanderPoint = enemy.transform.position;
        _currentDirection = Vector2.zero;
    }

    public override void DoEnterLogic()
    {
        base.DoEnterLogic();

        enemy.animator.SetBool("IsMoving", true);
        _timer = 0f;
        _wanderPoint = GetRandomWanderPoint();
    }

    public override void DoExitLogic()
    {
        base.DoExitLogic();

        enemy.animator.SetBool("IsMoving", false);
        enemy.MoveEnemy(Vector2.zero);
        _currentDirection = Vector2.zero;
    }

    public override void DoFrameUpdateLogic()
    {
        base.DoFrameUpdateLogic();
        // All our movement is in FixedUpdate via DoPhysicsLogic.
    }

    public override void DoPhysicsLogic()
    {
        base.DoPhysicsLogic();

        _timer += Time.fixedDeltaTime;

        Vector2 currentPos = enemy.transform.position;
        _distance = Vector2.Distance(currentPos, _wanderPoint);

        // --- Compute desired direction using pathfinding ---
        Vector2 desiredDir = Vector2.zero;

        if (_pathAgent != null)
        {
            desiredDir = _pathAgent.GetMoveDirection(_wanderPoint);
        }

        // Fallback: go straight to the point if no path / nav
        if (desiredDir.sqrMagnitude < 0.0001f)
        {
            Vector2 direct = _wanderPoint - currentPos;
            if (direct.sqrMagnitude > 0.0001f)
                desiredDir = direct.normalized;
        }

        // Smooth turning a bit so it doesn't snap
        if (desiredDir.sqrMagnitude > 0.0001f)
        {
            _currentDirection = Vector2.Lerp(
                _currentDirection,
                desiredDir.normalized,
                DirectionLerpSpeed * Time.fixedDeltaTime
            );

            enemy.MoveEnemy(_currentDirection.normalized * enemy.WalkSpeed);
            enemy.animator.SetBool("IsMoving", true);
        }
        else
        {
            enemy.MoveEnemy(Vector2.zero);
            enemy.animator.SetBool("IsMoving", false);
        }

        // --- End of wander: tell the FSM we're done wandering ---
        if (_distance <= 0.1f || _timer >= WanderTimer)
        {
            enemy.isWondering = false;
        }

        enemy.ForcedIdleCalclulation(Time.fixedDeltaTime);
    }

    public override void DoAnimationTriggerEventLogic(Enemy.AnimationTriggerType triggerType)
    {
        base.DoAnimationTriggerEventLogic(triggerType);
    }

    public override void ResetValues()
    {
        base.ResetValues();
        _timer = 0f;
        _currentDirection = Vector2.zero;
    }

    private Vector2 GetRandomWanderPoint()
    {
        Vector2 origin = enemy.transform.position;

        if (TileNavWorld.Instance == null)
        {
            return origin + Random.insideUnitCircle * WanderRadius;
        }

        for (int i = 0; i < 10; i++)
        {
            Vector2 candidate = origin + Random.insideUnitCircle * WanderRadius;

            if (TileNavWorld.Instance.IsWalkableWorldPos(candidate))
                return candidate;
        }

        return origin;
    }
}
