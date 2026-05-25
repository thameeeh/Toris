using OutlandHaven.Inventory;
using UnityEngine;

public static class EnemyLootRuntime
{
    private const float DropScatterMinRadius = 0.55f;
    private const float DropScatterMaxRadius = 1.15f;
    private const float DropHeightOffset = 0.2f;
    private const float DropTriggerRadius = 0.3f;
    private const float DropSpriteScale = 0.8f;
    private const int DropItemSortingOrder = 0;
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
        {
            playerProgression.AddGold(goldReward);
            PlayGoldRewardSfx(lootTable.GoldRewardSfxId);
        }

        int xpReward = RollInclusive(lootTable.MinXp, lootTable.MaxXp);
        if (xpReward > 0)
            playerProgression.AddExperience(xpReward);
    }

    private static void PlayGoldRewardSfx(string sfxId)
    {
        // SFX-only hook: called after immediate gold has already been granted.
        // Loot, XP, and progression state must stay owned by the reward logic above.
        if (AudioBootstrap.Sfx == null || string.IsNullOrWhiteSpace(sfxId))
            return;

        SfxPlayRequest request = SfxPlayRequest.Default;
        request.force2D = true;
        AudioBootstrap.Sfx.Play(sfxId, request);
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

            Vector3 startPosition = GetDropStartPosition(origin);
            Vector3 landingPosition = GetDropLandingPosition(origin);
            SpawnWorldItemDrop(
                itemDrop.Item,
                quantity,
                startPosition,
                landingPosition,
                lootTable.DropGlowPrefab,
                lootTable.DropShadowPrefab);
        }
    }

    private static void SpawnWorldItemDrop(
        InventoryItemSO item,
        int quantity,
        Vector3 startPosition,
        Vector3 landingPosition,
        GameObject glowPrefab,
        GameObject shadowPrefab)
    {
        GameObject dropObject = new GameObject($"WorldItem_{item.ItemName}");
        dropObject.transform.position = landingPosition;
        dropObject.layer = GetItemLayer();

        SpriteRenderer rootRenderer = dropObject.AddComponent<SpriteRenderer>();
        rootRenderer.enabled = false;

        CircleCollider2D collider = dropObject.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = DropTriggerRadius;

        WorldItem worldItem = dropObject.AddComponent<WorldItem>();
        SpriteRenderer itemRenderer = CreateItemVisual(dropObject.transform, item);
        worldItem.SetVisualRenderer(itemRenderer);
        worldItem.Initialize(item, quantity);

        WorldItemDropPresentation presentation = dropObject.AddComponent<WorldItemDropPresentation>();
        presentation.Initialize(
            itemRenderer.transform,
            collider,
            startPosition,
            landingPosition,
            glowPrefab,
            shadowPrefab,
            DropItemSortingOrder);

        dropObject.AddComponent<WorldItemMagnet>();
    }

    private static SpriteRenderer CreateItemVisual(Transform parent, InventoryItemSO item)
    {
        GameObject visualObject = new GameObject("Visual");
        visualObject.transform.SetParent(parent, false);
        visualObject.transform.localScale = Vector3.one * DropSpriteScale;

        SpriteRenderer itemRenderer = visualObject.AddComponent<SpriteRenderer>();
        itemRenderer.sprite = item.Icon;
        itemRenderer.sortingOrder = DropItemSortingOrder;
        itemRenderer.spriteSortPoint = SpriteSortPoint.Pivot;
        return itemRenderer;
    }

    private static Vector3 GetDropStartPosition(Vector3 origin)
    {
        return new Vector3(
            origin.x,
            origin.y + DropHeightOffset,
            0f);
    }

    private static Vector3 GetDropLandingPosition(Vector3 origin)
    {
        Vector2 direction = Random.insideUnitCircle.normalized;
        if (direction.sqrMagnitude <= 0.001f)
            direction = Vector2.right;

        float scatterDistance = Random.Range(DropScatterMinRadius, DropScatterMaxRadius);
        Vector2 scatter = direction * scatterDistance;
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
