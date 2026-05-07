using UnityEngine;

public static class BowAbilityTargetingUtility
{
    private const string EnemyHurtBoxLayerName = "EnemyHurtBox";
    public const int DamageableOverlapResultCapacity = 64;

    public static int GetEnemyHurtBoxLayer()
    {
        return LayerMask.NameToLayer(EnemyHurtBoxLayerName);
    }

    public static int GetEnemyHurtBoxMask()
    {
        int enemyHurtBoxLayer = GetEnemyHurtBoxLayer();
        return enemyHurtBoxLayer >= 0
            ? 1 << enemyHurtBoxLayer
            : Physics2D.DefaultRaycastLayers;
    }

    public static bool IsEnemyHurtBoxCollider(Collider2D overlapCollider)
    {
        if (overlapCollider == null)
            return false;

        int enemyHurtBoxLayer = GetEnemyHurtBoxLayer();
        if (enemyHurtBoxLayer < 0)
            return true;

        return overlapCollider.gameObject.layer == enemyHurtBoxLayer;
    }

    public static int GetDamageableQueryMask()
    {
        return Physics2D.DefaultRaycastLayers;
    }

    public static bool TryResolveDamageable(
        Collider2D overlapCollider,
        out IDamageable damageable,
        out Component damageableComponent)
    {
        damageable = null;
        damageableComponent = null;

        if (overlapCollider == null)
            return false;

        if (!IsDamageableHitCollider(overlapCollider))
            return false;

        damageable = overlapCollider.GetComponentInParent<IDamageable>();
        if (damageable == null)
            return false;

        damageableComponent = damageable as Component;
        return true;
    }

    private static bool IsDamageableHitCollider(Collider2D overlapCollider)
    {
        if (overlapCollider == null)
            return false;

        if (!overlapCollider.isTrigger)
            return true;

        return IsEnemyHurtBoxCollider(overlapCollider);
    }
}
