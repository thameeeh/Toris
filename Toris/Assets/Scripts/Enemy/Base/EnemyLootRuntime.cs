using OutlandHaven.Inventory;
using UnityEngine;

public static class EnemyLootRuntime
{
    private const float DropScatterRadius = 0.65f;
    private const float DropHeightOffset = 0.2f;
    private const float DropTriggerRadius = 0.3f;
    private const float DropSpriteScale = 0.8f;
    private const int FallbackItemLayer = 17;

    public static void ResolveDeathLoot(Enemy enemy, PlayerProgression playerProgression)
    {
        if (enemy == null)
            return;

        EnemyLootTableSO lootTable = enemy.LootTable;
        if (lootTable == null)
            return;

        PlayerProgression resolvedProgression = ResolvePlayerProgression(enemy, playerProgression);
        GrantImmediateRewards(lootTable, resolvedProgression);
        SpawnItemDrops(lootTable, enemy.transform.position);
    }

    private static PlayerProgression ResolvePlayerProgression(Enemy enemy, PlayerProgression playerProgression)
    {
        if (playerProgression != null)
            return playerProgression;

        Transform playerTransform = enemy.PlayerTransform;
        if (playerTransform == null)
            return null;

        playerTransform.TryGetComponent(out PlayerProgression resolvedProgression);
        return resolvedProgression;
    }

    private static void GrantImmediateRewards(EnemyLootTableSO lootTable, PlayerProgression playerProgression)
    {
        if (playerProgression == null)
            return;

        int goldReward = RollInclusive(lootTable.MinGold, lootTable.MaxGold);
        if (goldReward > 0)
            playerProgression.AddGold(goldReward);

        int xpReward = RollInclusive(lootTable.MinXp, lootTable.MaxXp);
        if (xpReward > 0)
            playerProgression.AddExperience(xpReward);
    }

    private static void SpawnItemDrops(EnemyLootTableSO lootTable, Vector3 origin)
    {
        var itemDrops = lootTable.ItemDrops;
        if (itemDrops == null || itemDrops.Count == 0)
            return;

        for (int i = 0; i < itemDrops.Count; i++)
        {
            EnemyLootItemEntry itemDrop = itemDrops[i];
            if (itemDrop == null || itemDrop.Item == null)
                continue;

            if (!RollChance(itemDrop.DropChance))
                continue;

            int quantity = RollInclusive(itemDrop.MinQuantity, itemDrop.MaxQuantity);
            if (quantity <= 0)
                continue;

            SpawnWorldItemDrop(itemDrop.Item, quantity, GetDropPosition(origin));
        }
    }

    private static void SpawnWorldItemDrop(InventoryItemSO item, int quantity, Vector3 position)
    {
        GameObject dropObject = new GameObject($"WorldItem_{item.ItemName}");
        dropObject.transform.position = position;
        dropObject.layer = GetItemLayer();

        SpriteRenderer spriteRenderer = dropObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = item.Icon;
        dropObject.transform.localScale = Vector3.one * DropSpriteScale;

        CircleCollider2D collider = dropObject.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = DropTriggerRadius;

        WorldItem worldItem = dropObject.AddComponent<WorldItem>();
        worldItem.Initialize(item, quantity);
    }

    private static Vector3 GetDropPosition(Vector3 origin)
    {
        Vector2 scatter = Random.insideUnitCircle * DropScatterRadius;
        return new Vector3(
            origin.x + scatter.x,
            origin.y + scatter.y + DropHeightOffset,
            0f);
    }

    private static bool RollChance(float chance)
    {
        if (chance <= 0f)
            return false;

        if (chance >= 1f)
            return true;

        return Random.value <= chance;
    }

    private static int RollInclusive(int minValue, int maxValue)
    {
        int clampedMin = Mathf.Max(0, minValue);
        int clampedMax = Mathf.Max(clampedMin, maxValue);
        return Random.Range(clampedMin, clampedMax + 1);
    }

    private static int GetItemLayer()
    {
        int itemLayer = LayerMask.NameToLayer("Item");
        return itemLayer >= 0 ? itemLayer : FallbackItemLayer;
    }
}
