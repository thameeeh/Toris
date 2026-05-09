using UnityEngine;

public class EnemyStrikingDistanceCheck : MonoBehaviour
{
    private readonly System.Collections.Generic.List<IEnemyAggroTarget> trackedTargets = new System.Collections.Generic.List<IEnemyAggroTarget>();

    [SerializeField] private bool detectPlayer = true;
    [SerializeField] private bool detectPassivePrey;

    private Enemy _enemy;

    private void Awake()
    {
        _enemy = GetComponentInParent<Enemy>();
        if (_enemy != null)
            _enemy.AggroTargetChanged += HandleAggroTargetChanged;
    }

    private void OnDestroy()
    {
        if (_enemy != null)
            _enemy.AggroTargetChanged -= HandleAggroTargetChanged;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (_enemy == null || !TryResolveTarget(collision, out IEnemyAggroTarget target))
            return;

        TrackTarget(target);
        RefreshStrikingDistance();
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (_enemy == null || !TryResolveTarget(collision, out IEnemyAggroTarget target))
            return;

        UntrackTarget(target);
        RefreshStrikingDistance();
    }

    private bool TryResolveTarget(Collider2D collision, out IEnemyAggroTarget target)
    {
        target = null;
        if (collision == null)
            return false;

        if (detectPlayer && TryResolvePlayerTarget(collision, out target))
            return true;

        target = collision.GetComponentInParent<IEnemyAggroTarget>();
        if (target == null || ReferenceEquals(target, _enemy))
            return false;

        if (target is PlayerDamageReceiver)
            return detectPlayer;

        Enemy targetEnemy = target as Enemy;
        if (targetEnemy != null)
            return detectPassivePrey && targetEnemy.IsPassivePrey
                || _enemy.IsCurrentAggroTarget(target);

        return true;
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

    private void TrackTarget(IEnemyAggroTarget target)
    {
        UntrackTarget(target);
        trackedTargets.Add(target);
    }

    private void UntrackTarget(IEnemyAggroTarget target)
    {
        if (target == null)
            return;

        for (int i = trackedTargets.Count - 1; i >= 0; i--)
        {
            if (AreSameTarget(trackedTargets[i], target))
                trackedTargets.RemoveAt(i);
        }
    }

    private void RefreshStrikingDistance()
    {
        RemoveInvalidTargets();

        bool hasCurrentTargetInRange = false;
        for (int i = 0; i < trackedTargets.Count; i++)
        {
            if (_enemy.IsCurrentAggroTarget(trackedTargets[i]))
            {
                hasCurrentTargetInRange = true;
                break;
            }
        }

        _enemy.SetStrikingDistanceBool(hasCurrentTargetInRange);
    }

    private void RemoveInvalidTargets()
    {
        for (int i = trackedTargets.Count - 1; i >= 0; i--)
        {
            if (!_enemy.IsAggroTargetValid(trackedTargets[i]))
                trackedTargets.RemoveAt(i);
        }
    }

    private void HandleAggroTargetChanged(IEnemyAggroTarget target)
    {
        RefreshStrikingDistance();
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
}
