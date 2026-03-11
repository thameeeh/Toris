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

    private Enemy _enemy;

    private readonly List<Vector3> _currentPath = new List<Vector3>();
    private int _pathIndex;
    private float _repathTimer;
    private Vector3 _lastTarget;
    private bool _hasLastTarget;
    private bool _hasValidPath;

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
        if (TileNavWorld.Instance == null)
        {
            if (allowDirectFallbackWhenNoPath)
            {
                Vector2 dirFallback = desiredTargetWorld - transform.position;
                return dirFallback.sqrMagnitude > 0.0001f ? dirFallback.normalized : Vector2.zero;
            }

            return Vector2.zero;
        }

        _repathTimer -= Time.deltaTime;

        bool needRepath = false;

        if (!_hasLastTarget)
        {
            needRepath = true;
        }
        else
        {
            float distToLastTarget = (desiredTargetWorld - _lastTarget).sqrMagnitude;
            if (distToLastTarget > targetChangeThreshold * targetChangeThreshold)
                needRepath = true;
        }

        if (_repathTimer <= 0f)
        {
            needRepath = true;
        }

        if (needRepath)
        {
            RecalculatePath(desiredTargetWorld);
            _repathTimer = repathInterval;
            _lastTarget = desiredTargetWorld;
            _hasLastTarget = true;
        }

        if (!_hasValidPath || _currentPath.Count == 0)
        {
            if (allowDirectFallbackWhenNoPath)
            {
                Vector2 direct = desiredTargetWorld - transform.position;
                return direct.sqrMagnitude > 0.0001f ? direct.normalized : Vector2.zero;
            }

            return Vector2.zero;
        }

        if (_pathIndex < 0 || _pathIndex >= _currentPath.Count)
        {
            _hasValidPath = false;
            return Vector2.zero;
        }

        Vector3 waypoint = _currentPath[_pathIndex];
        Vector2 toWaypoint = waypoint - transform.position;

        if (toWaypoint.sqrMagnitude < waypointReachThreshold * waypointReachThreshold)
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
                return Vector2.zero;
            }
        }

        if (toWaypoint.sqrMagnitude > 0.0001f)
            return toWaypoint.normalized;

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
}
