using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Enemy))]
public class GridPathAgent : MonoBehaviour
{
    [Header("Pathfinding")]
    [SerializeField] private float repathInterval = 0.4f;
    [SerializeField] private int maxPathRange = 25;
    [SerializeField] private float waypointReachThreshold = 0.1f;
    [SerializeField] private float targetChangeThreshold = 0.5f;

    [Header("Behavior When No Path")]
    [Tooltip("If true, when no path is found the agent will still walk straight towards the target (ignoring nav). " +
             "If false, the agent will STOP when no path exists.")]
    [SerializeField] private bool allowDirectFallbackWhenNoPath = false;
#if UNITY_EDITOR
    [Header("Debug")]
    [SerializeField] private bool debugPathing;
#endif

    private Enemy _enemy;

    private readonly List<Vector3> _currentPath = new List<Vector3>();
    private int _pathIndex;
    private float _repathTimer;
    private Vector3 _lastTarget;
    private bool _hasLastTarget;
    private bool _hasValidPath;
    private Vector2 _lastReturnedDirection = Vector2.zero;

#if UNITY_EDITOR
    private const float DebugPathLogInterval = 0.1f;
    private string _lastDebugMessage = string.Empty;
    private float _nextDebugLogTime;
#endif

    private void Awake()
    {
        _enemy = GetComponent<Enemy>();
    }

    private void OnEnable()
    {
        _currentPath.Clear();
        _pathIndex = 0;
        _repathTimer = 0f;
        _hasLastTarget = false;
        _hasValidPath = false;
    }

    /// <summary>
    /// Get move direction towards desiredTargetWorld using a cached path.
    /// Will only recompute path occasionally or when the target changed a lot.
    /// </summary>
    public Vector2 GetMoveDirection(Vector3 desiredTargetWorld)
    {
        string repathReason = string.Empty;

        if (TileNavWorld.Instance == null)
        {
            if (allowDirectFallbackWhenNoPath)
            {
                Vector2 dirFallback = desiredTargetWorld - transform.position;
                LogPathing($"NoTileNav direct-fallback target={desiredTargetWorld} dir={dirFallback.normalized}");
                return dirFallback.sqrMagnitude > 0.0001f ? dirFallback.normalized : Vector2.zero;
            }

            LogPathing($"NoTileNav stop target={desiredTargetWorld}");
            return Vector2.zero;
        }

        _repathTimer -= Time.fixedDeltaTime;

        bool needRepath = false;

        if (!_hasLastTarget)
        {
            needRepath = true;
            repathReason = "NoLastTarget";
        }
        else
        {
            float distToLastTarget = (desiredTargetWorld - _lastTarget).sqrMagnitude;
            if (distToLastTarget > targetChangeThreshold * targetChangeThreshold)
            {
                needRepath = true;
                repathReason = "TargetMoved";
            }
        }

        if (_repathTimer <= 0f)
        {
            needRepath = true;
            repathReason = string.IsNullOrEmpty(repathReason) ? "Interval" : $"{repathReason}+Interval";
        }

        if (needRepath)
        {
            RecalculatePath(desiredTargetWorld);
            _repathTimer = repathInterval;
            _lastTarget = desiredTargetWorld;
            _hasLastTarget = true;
            LogPathing(
                $"Repath reason={repathReason} valid={_hasValidPath} " +
                $"count={_currentPath.Count} pathIndex={_pathIndex} target={desiredTargetWorld}");
        }

        if (!_hasValidPath || _currentPath.Count == 0)
        {
            if (allowDirectFallbackWhenNoPath)
            {
                Vector2 direct = desiredTargetWorld - transform.position;
                LogPathing($"NoPath direct-fallback target={desiredTargetWorld} dir={direct.normalized}");
                return direct.sqrMagnitude > 0.0001f ? direct.normalized : Vector2.zero;
            }

            LogPathing($"NoPath stop target={desiredTargetWorld}");
            return Vector2.zero;
        }

        if (_pathIndex < 0 || _pathIndex >= _currentPath.Count)
        {
            _hasValidPath = false;
            LogPathing($"InvalidPathIndex stop pathIndex={_pathIndex} count={_currentPath.Count}");
            return Vector2.zero;
        }

        Vector3 waypoint = _currentPath[_pathIndex];
        Vector2 toWaypoint = waypoint - transform.position;

        float reachSqr = waypointReachThreshold * waypointReachThreshold;

        while (toWaypoint.sqrMagnitude < reachSqr)
        {
            if (_pathIndex < _currentPath.Count - 1)
            {
                _pathIndex++;
                waypoint = _currentPath[_pathIndex];
                toWaypoint = waypoint - transform.position;
            }
            else
            {
                _hasValidPath = false;
                LogPathing($"PathFinished stop target={desiredTargetWorld}");
                return Vector2.zero;
            }
        }

        if (toWaypoint.sqrMagnitude > 0.0001f)
        {
            Vector2 result = toWaypoint.normalized;
            LogDirectionFlip(result, waypoint, desiredTargetWorld);
            _lastReturnedDirection = result;
            return result;
        }

        LogPathing($"ZeroDirection stop waypoint={waypoint} target={desiredTargetWorld}");
        return Vector2.zero;
    }

    private void RecalculatePath(Vector3 desiredTargetWorld)
    {
        _currentPath.Clear();
        _pathIndex = 0;
        _hasValidPath = false;

        bool success = TilePathfinder.TryFindPath(
            transform.position,
            desiredTargetWorld,
            _currentPath,
            maxPathRange);

        if (!success || _currentPath.Count == 0)
        {
            _currentPath.Clear();
            _pathIndex = 0;
            _hasValidPath = false;

            //Debug.Log($"[GridPathAgent] No path found for {_enemy?.name ?? gameObject.name} to target at {desiredTargetWorld}.");
        }
        else
        {
            _hasValidPath = true;

            for (int i = 0; i < _currentPath.Count - 1; i++)
            {
                Debug.DrawLine(_currentPath[i], _currentPath[i + 1], Color.cyan, 0.5f);
            }
        }
    }

    private void LogDirectionFlip(Vector2 newDirection, Vector3 waypoint, Vector3 desiredTargetWorld)
    {
#if UNITY_EDITOR
        if (!ShouldDebugPathing())
            return;

        if (_lastReturnedDirection.sqrMagnitude <= 0.0001f || newDirection.sqrMagnitude <= 0.0001f)
            return;

        float dot = Vector2.Dot(_lastReturnedDirection.normalized, newDirection.normalized);
        if (dot > -0.2f)
            return;

        LogPathing(
            $"DirectionFlip dot={dot:0.##} pathIndex={_pathIndex}/{_currentPath.Count} " +
            $"waypoint={waypoint} target={desiredTargetWorld} current={transform.position}");
#endif
    }

    private void LogPathing(string message)
    {
#if UNITY_EDITOR
        if (!ShouldDebugPathing())
            return;

        float now = Time.time;
        if (_lastDebugMessage == message && now < _nextDebugLogTime)
            return;

        _lastDebugMessage = message;
        _nextDebugLogTime = now + DebugPathLogInterval;
        Debug.Log($"[GridPath:{_enemy.name}] {message}", _enemy);
#endif
    }
#if UNITY_EDITOR
    private bool ShouldDebugPathing()
    {
        return debugPathing && _enemy is Necromancer;
    }
#endif
}
