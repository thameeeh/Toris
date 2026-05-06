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

        UntrackTarget(target.TargetTransform);
        ApplyBestTarget();
    }

    private bool TryResolveTarget(Collider2D collision, out TrackedTarget target)
    {
        target = default;
        if (collision == null)
            return false;

        if (detectPlayer && TryResolvePlayerTarget(collision, out target))
            return true;

        Enemy targetEnemy = collision.GetComponentInParent<Enemy>();
        if (!IsValidEnemyTarget(targetEnemy))
            return false;

        if (detectThreatsToPassiveCreatures && targetEnemy.ThreatensPassiveCreatures)
        {
            target = new TrackedTarget(targetEnemy.transform, targetEnemy, ThreatTargetPriority);
            return true;
        }

        if (detectPassivePrey && targetEnemy.IsPassivePrey)
        {
            target = new TrackedTarget(targetEnemy.transform, targetEnemy, PreyTargetPriority);
            return true;
        }

        return false;
    }

    private static bool TryResolvePlayerTarget(Collider2D collision, out TrackedTarget target)
    {
        target = default;

        if (collision.CompareTag("Player"))
        {
            target = new TrackedTarget(collision.transform, null, PlayerTargetPriority);
            return true;
        }

        PlayerDamageReceiver damageReceiver = collision.GetComponentInParent<PlayerDamageReceiver>();
        if (damageReceiver == null)
            return false;

        target = new TrackedTarget(damageReceiver.transform, null, PlayerTargetPriority);
        return true;
    }

    private bool IsValidEnemyTarget(Enemy targetEnemy)
    {
        return targetEnemy != null
            && targetEnemy != _enemy
            && targetEnemy.CurrentHealth > 0f
            && targetEnemy.gameObject.scene.IsValid();
    }

    private void TrackTarget(TrackedTarget target)
    {
        UntrackTarget(target.TargetTransform);
        trackedTargets.Add(target);
    }

    private void UntrackTarget(Transform targetTransform)
    {
        if (targetTransform == null)
            return;

        for (int i = trackedTargets.Count - 1; i >= 0; i--)
        {
            Transform trackedTransform = trackedTargets[i].TargetTransform;
            if (trackedTransform == targetTransform || trackedTransform != null && trackedTransform.root == targetTransform.root)
                trackedTargets.RemoveAt(i);
        }
    }

    private void ApplyBestTarget()
    {
        RemoveInvalidTargets();

        if (trackedTargets.Count == 0)
        {
            _enemy.SetAggroStatus(false);
            return;
        }

        TrackedTarget bestTarget = trackedTargets[0];
        for (int i = 1; i < trackedTargets.Count; i++)
        {
            if (trackedTargets[i].Priority > bestTarget.Priority)
                bestTarget = trackedTargets[i];
        }

        _enemy.SetAggroTarget(bestTarget.TargetTransform, bestTarget.TargetEnemy);
    }

    private void RemoveInvalidTargets()
    {
        for (int i = trackedTargets.Count - 1; i >= 0; i--)
        {
            TrackedTarget target = trackedTargets[i];
            if (target.TargetTransform == null || !target.TargetTransform.gameObject.scene.IsValid())
            {
                trackedTargets.RemoveAt(i);
                continue;
            }

            if (target.TargetEnemy != null && target.TargetEnemy.CurrentHealth <= 0f)
                trackedTargets.RemoveAt(i);
        }
    }

    private readonly struct TrackedTarget
    {
        public readonly Transform TargetTransform;
        public readonly Enemy TargetEnemy;
        public readonly int Priority;

        public TrackedTarget(Transform targetTransform, Enemy targetEnemy, int priority)
        {
            TargetTransform = targetTransform;
            TargetEnemy = targetEnemy;
            Priority = priority;
        }
    }
}
