using UnityEngine;

public class EnemyAggroCheck : MonoBehaviour
{
    private const int PlayerTargetPriority = 100;
    private const int ThreatTargetPriority = 80;
    private const int PreyTargetPriority = 60;

    private readonly System.Collections.Generic.List<TrackedTarget> trackedTargets = new System.Collections.Generic.List<TrackedTarget>();

    [SerializeField] private bool detectPlayer = true;
    [SerializeField] private bool detectPassivePrey;
    [SerializeField] private bool detectThreatsToPassiveCreatures;

    private Enemy _enemy;

    private void Awake()
    {
        _enemy = GetComponentInParent<Enemy>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (_enemy == null || !TryResolveTarget(collision, out TrackedTarget target))
            return;

        TrackTarget(target);
        ApplyBestTarget();
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (_enemy == null || !TryResolveTarget(collision, out TrackedTarget target))
            return;

        UntrackTarget(target.Target);
        ApplyBestTarget();
    }

    private bool TryResolveTarget(Collider2D collision, out TrackedTarget target)
    {
        target = default;
        if (collision == null)
            return false;

        if (detectPlayer && TryResolvePlayerTarget(collision, out IEnemyAggroTarget playerTarget))
        {
            target = new TrackedTarget(playerTarget, PlayerTargetPriority);
            return true;
        }

        IEnemyAggroTarget aggroTarget = collision.GetComponentInParent<IEnemyAggroTarget>();
        if (!IsValidAggroTarget(aggroTarget))
            return false;

        Enemy targetEnemy = aggroTarget as Enemy;
        if (targetEnemy == null)
            return false;

        if (detectThreatsToPassiveCreatures && targetEnemy.ThreatensPassiveCreatures)
        {
            target = new TrackedTarget(aggroTarget, ThreatTargetPriority);
            return true;
        }

        if (detectPassivePrey && targetEnemy.IsPassivePrey)
        {
            target = new TrackedTarget(aggroTarget, PreyTargetPriority);
            return true;
        }

        return false;
    }

    private static bool TryResolvePlayerTarget(Collider2D collision, out IEnemyAggroTarget target)
    {
        target = null;

        PlayerDamageReceiver damageReceiver = collision.GetComponentInParent<PlayerDamageReceiver>();
        if (damageReceiver != null)
        {
            target = damageReceiver;
            return true;
        }

        if (!collision.CompareTag("Player"))
            return false;

        target = collision.GetComponentInParent<IEnemyAggroTarget>();
        return target != null;
    }

    private bool IsValidAggroTarget(IEnemyAggroTarget target)
    {
        return target != null
            && !ReferenceEquals(target, _enemy)
            && _enemy.IsAggroTargetValid(target);
    }

    private void TrackTarget(TrackedTarget target)
    {
        UntrackTarget(target.Target);
        trackedTargets.Add(target);
    }

    private void UntrackTarget(IEnemyAggroTarget target)
    {
        if (target == null)
            return;

        for (int i = trackedTargets.Count - 1; i >= 0; i--)
        {
            if (AreSameTarget(trackedTargets[i].Target, target))
                trackedTargets.RemoveAt(i);
        }
    }

    private void ApplyBestTarget()
    {
        RemoveInvalidTargets();

        if (trackedTargets.Count == 0)
        {
            _enemy.ClearSensorAggroTarget();
            _enemy.SetAggroStatus(false);
            return;
        }

        TrackedTarget bestTarget = trackedTargets[0];
        for (int i = 1; i < trackedTargets.Count; i++)
        {
            if (trackedTargets[i].Priority > bestTarget.Priority)
                bestTarget = trackedTargets[i];
        }

        _enemy.SetSensorAggroTarget(bestTarget.Target);
    }

    private void RemoveInvalidTargets()
    {
        for (int i = trackedTargets.Count - 1; i >= 0; i--)
        {
            TrackedTarget target = trackedTargets[i];
            if (!IsValidAggroTarget(target.Target))
                trackedTargets.RemoveAt(i);
        }
    }

    private static bool AreSameTarget(IEnemyAggroTarget left, IEnemyAggroTarget right)
    {
        if (ReferenceEquals(left, right))
            return true;

        if (left == null || right == null)
            return false;

        Transform leftTransform = left.TargetTransform;
        Transform rightTransform = right.TargetTransform;
        if (leftTransform == null || rightTransform == null)
            return false;

        return leftTransform == rightTransform || leftTransform.root == rightTransform.root;
    }

    private readonly struct TrackedTarget
    {
        public readonly IEnemyAggroTarget Target;
        public readonly int Priority;

        public TrackedTarget(IEnemyAggroTarget target, int priority)
        {
            Target = target;
            Priority = priority;
        }
    }
}
