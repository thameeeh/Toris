using UnityEngine;

public class NecromancerCastingRangeCheck : MonoBehaviour
{
    private readonly System.Collections.Generic.List<IEnemyAggroTarget> trackedTargets = new System.Collections.Generic.List<IEnemyAggroTarget>();
    private Necromancer _necromancer;

    private void Awake()
    {
        _necromancer = GetComponentInParent<Necromancer>();
        if (_necromancer != null)
            _necromancer.AggroTargetChanged += HandleAggroTargetChanged;
    }

    private void OnDestroy()
    {
        if (_necromancer != null)
            _necromancer.AggroTargetChanged -= HandleAggroTargetChanged;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (_necromancer == null || !TryResolveTarget(collision, out IEnemyAggroTarget target))
            return;

        TrackTarget(target);
        RefreshCastingRange();
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (_necromancer == null || !TryResolveTarget(collision, out IEnemyAggroTarget target))
            return;

        UntrackTarget(target);
        RefreshCastingRange();
    }

    private static bool TryResolveTarget(Collider2D collision, out IEnemyAggroTarget target)
    {
        target = null;
        if (collision == null)
            return false;

        PlayerDamageReceiver damageReceiver = collision.GetComponentInParent<PlayerDamageReceiver>();
        if (damageReceiver != null)
        {
            target = damageReceiver;
            return true;
        }

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

    private void RefreshCastingRange()
    {
        RemoveInvalidTargets();

        bool hasCurrentTargetInRange = false;
        for (int i = 0; i < trackedTargets.Count; i++)
        {
            if (_necromancer.IsCurrentAggroTarget(trackedTargets[i]))
            {
                hasCurrentTargetInRange = true;
                break;
            }
        }

        _necromancer.SetCastingRangeBool(hasCurrentTargetInRange);
    }

    private void RemoveInvalidTargets()
    {
        for (int i = trackedTargets.Count - 1; i >= 0; i--)
        {
            if (!_necromancer.IsAggroTargetValid(trackedTargets[i]))
                trackedTargets.RemoveAt(i);
        }
    }

    private void HandleAggroTargetChanged(IEnemyAggroTarget target)
    {
        RefreshCastingRange();
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
