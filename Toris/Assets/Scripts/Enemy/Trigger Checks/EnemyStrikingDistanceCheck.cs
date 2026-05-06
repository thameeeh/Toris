using UnityEngine;

public class EnemyStrikingDistanceCheck : MonoBehaviour
{
    private readonly System.Collections.Generic.List<Transform> trackedTargets = new System.Collections.Generic.List<Transform>();

    [SerializeField] private bool detectPlayer = true;
    [SerializeField] private bool detectPassivePrey;

    private Enemy _enemy;

    private void Awake()
    {
        _enemy = GetComponentInParent<Enemy>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (_enemy == null || !TryResolveTarget(collision, out Transform targetTransform))
            return;

        TrackTarget(targetTransform);
        _enemy.SetStrikingDistanceBool(trackedTargets.Count > 0);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (_enemy == null || !TryResolveTarget(collision, out Transform targetTransform))
            return;

        UntrackTarget(targetTransform);
        _enemy.SetStrikingDistanceBool(trackedTargets.Count > 0);
    }

    private bool TryResolveTarget(Collider2D collision, out Transform targetTransform)
    {
        targetTransform = null;
        if (collision == null)
            return false;

        if (detectPlayer && TryResolvePlayerTarget(collision, out targetTransform))
            return true;

        Enemy targetEnemy = collision.GetComponentInParent<Enemy>();
        if (targetEnemy == null || targetEnemy == _enemy || targetEnemy.CurrentHealth <= 0f)
            return false;

        if (!detectPassivePrey || !targetEnemy.IsPassivePrey)
            return false;

        targetTransform = targetEnemy.transform;
        return true;
    }

    private static bool TryResolvePlayerTarget(Collider2D collision, out Transform targetTransform)
    {
        targetTransform = null;

        if (collision.CompareTag("Player"))
        {
            targetTransform = collision.transform;
            return true;
        }

        PlayerDamageReceiver damageReceiver = collision.GetComponentInParent<PlayerDamageReceiver>();
        if (damageReceiver == null)
            return false;

        targetTransform = damageReceiver.transform;
        return true;
    }

    private void TrackTarget(Transform targetTransform)
    {
        UntrackTarget(targetTransform);
        trackedTargets.Add(targetTransform);
    }

    private void UntrackTarget(Transform targetTransform)
    {
        if (targetTransform == null)
            return;

        for (int i = trackedTargets.Count - 1; i >= 0; i--)
        {
            Transform trackedTransform = trackedTargets[i];
            if (trackedTransform == targetTransform || trackedTransform != null && trackedTransform.root == targetTransform.root)
                trackedTargets.RemoveAt(i);
        }
    }
}
