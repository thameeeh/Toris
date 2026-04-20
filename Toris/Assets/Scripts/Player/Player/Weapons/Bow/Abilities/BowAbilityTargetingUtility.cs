using UnityEngine;

public static class BowAbilityTargetingUtility
{
    public const string EnemyHurtBoxLayerName = "EnemyHurtBox";

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
}
