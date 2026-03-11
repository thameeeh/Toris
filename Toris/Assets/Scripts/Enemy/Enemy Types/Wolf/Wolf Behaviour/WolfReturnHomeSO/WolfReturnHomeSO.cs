using UnityEngine;

[CreateAssetMenu(fileName = "Wolf_ReturnHome", menuName = "Enemy Logic/Return Logic/Wolf Return Home")]
public class WolfReturnHomeSO : EnemyBehaviourSO<Wolf>
{
    [Header("Arrival")]
    [SerializeField] private float arriveDistance = 0.75f;
    [SerializeField] private float insideHomeRadiusMultiplier = 0.85f;

    [Header("Target Selection")]
    [SerializeField] private float repickTargetInterval = 0.75f;
    [SerializeField] private float targetRingPadding = 1.5f;
    [SerializeField] private int maxCandidateChecks = 16;
    [SerializeField] private int nearestWalkableSearchRadius = 6;

    [Header("Movement")]
    [SerializeField] private float directionLerpSpeed = 12f;
    [SerializeField] private float minDirectionSqrMagnitude = 0.0001f;

    private GridPathAgent _pathAgent;
    private Vector2 _currentDirection;
    private Vector3 _returnTarget;
    private float _repickTimer;

    public bool HasArrived { get; private set; }

    public override void Initialize(GameObject gameObject, Wolf enemy, Transform player)
    {
        base.Initialize(gameObject, enemy, player);

        _pathAgent = enemy.GetComponent<GridPathAgent>();
        _currentDirection = Vector2.zero;
        _returnTarget = enemy.transform.position;
        _repickTimer = 0f;
        HasArrived = false;
    }

    public override void DoEnterLogic()
    {
        base.DoEnterLogic();

        HasArrived = false;
        _currentDirection = Vector2.zero;
        _repickTimer = 0f;

        PickReturnTarget();
        enemy.animator.SetBool("IsMoving", true);
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
    }

    public override void DoPhysicsLogic()
    {
        base.DoPhysicsLogic();

        if (!enemy.HasHome)
        {
            HasArrived = true;
            enemy.MoveEnemy(Vector2.zero);
            return;
        }

        _repickTimer -= Time.fixedDeltaTime;
        if (_repickTimer <= 0f)
        {
            PickReturnTarget();
            _repickTimer = repickTargetInterval;
        }

        float distToTarget = Vector2.Distance(enemy.transform.position, _returnTarget);
        bool closeToTarget = distToTarget <= arriveDistance;
        bool safelyInsideHome = enemy.DistanceToHome <= enemy.HomeRadius * insideHomeRadiusMultiplier;

        if (closeToTarget || safelyInsideHome)
        {
            HasArrived = true;
            enemy.MoveEnemy(Vector2.zero);
            enemy.animator.SetBool("IsMoving", false);
            return;
        }

        Vector2 desiredDir = Vector2.zero;

        if (_pathAgent != null)
            desiredDir = _pathAgent.GetMoveDirection(_returnTarget);

        if (desiredDir.sqrMagnitude < minDirectionSqrMagnitude)
        {
            Vector2 direct = _returnTarget - enemy.transform.position;
            if (direct.sqrMagnitude > minDirectionSqrMagnitude)
                desiredDir = direct.normalized;
        }

        if (desiredDir.sqrMagnitude > minDirectionSqrMagnitude)
        {
            _currentDirection = Vector2.Lerp(
                _currentDirection,
                desiredDir.normalized,
                directionLerpSpeed * Time.fixedDeltaTime
            );

            enemy.MoveEnemy(_currentDirection.normalized * enemy.MovementSpeed);
            enemy.animator.SetBool("IsMoving", true);
        }
        else
        {
            enemy.MoveEnemy(Vector2.zero);
            enemy.animator.SetBool("IsMoving", false);
        }
    }

    private void PickReturnTarget()
    {
        Vector3 home = enemy.HomeCenter;

        if (TileNavWorld.Instance == null)
        {
            _returnTarget = home;
            return;
        }

        float desiredRadius = Mathf.Max(arriveDistance, enemy.HomeRadius - targetRingPadding);

        for (int i = 0; i < maxCandidateChecks; i++)
        {
            Vector2 offset = Random.insideUnitCircle * desiredRadius;
            Vector3 candidate = home + new Vector3(offset.x, offset.y, 0f);

            if (!TileNavWorld.Instance.IsWalkableWorldPos(candidate))
                continue;

            _returnTarget = candidate;
            return;
        }

        _returnTarget = FindNearestWalkable(home, nearestWalkableSearchRadius);
    }

    private Vector3 FindNearestWalkable(Vector3 desiredWorldPos, int maxTileRadius)
    {
        var nav = TileNavWorld.Instance;
        if (nav == null)
            return desiredWorldPos;

        Vector2Int startCell = nav.WorldToCell(desiredWorldPos);

        if (nav.IsWalkableCell(startCell))
            return nav.CellToWorldCenter(startCell);

        for (int r = 1; r <= maxTileRadius; r++)
        {
            for (int x = -r; x <= r; x++)
            {
                var c1 = startCell + new Vector2Int(x, r);
                if (nav.IsWalkableCell(c1))
                    return nav.CellToWorldCenter(c1);

                var c2 = startCell + new Vector2Int(x, -r);
                if (nav.IsWalkableCell(c2))
                    return nav.CellToWorldCenter(c2);
            }

            for (int y = -r + 1; y <= r - 1; y++)
            {
                var c3 = startCell + new Vector2Int(r, y);
                if (nav.IsWalkableCell(c3))
                    return nav.CellToWorldCenter(c3);

                var c4 = startCell + new Vector2Int(-r, y);
                if (nav.IsWalkableCell(c4))
                    return nav.CellToWorldCenter(c4);
            }
        }

        return enemy.transform.position;
    }
}