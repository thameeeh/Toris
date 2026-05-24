using System;
using System.Collections.Generic;
using OutlandHaven.Inventory;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyLootTable", menuName = "Enemy/Loot/Enemy Loot Table")]
public class EnemyLootTableSO : ScriptableObject
{
    private const string DefaultGoldRewardSfxId = "item_coin_pickup";

    [SerializeField] private List<EnemyLootItemEntry> itemDrops = new List<EnemyLootItemEntry>();
    [SerializeField, Min(0)] private int minGold;
    [SerializeField, Min(0)] private int maxGold;
    [SerializeField, Min(0)] private int minXp;
    [SerializeField, Min(0)] private int maxXp;

    [Header("SFX")]
    // SFX-only authoring value. Runtime loot behavior should not depend on this field.
    [Tooltip("Played when this loot table grants immediate gold. Clear to disable.")]
    [SerializeField] private string goldRewardSfxId = DefaultGoldRewardSfxId;

    [Header("Drop Presentation")]
    [Tooltip("Optional glow prefab instantiated behind spawned world item icons.")]
    [SerializeField] private GameObject dropGlowPrefab;
    [Tooltip("Optional shadow prefab instantiated under spawned world item icons.")]
    [SerializeField] private GameObject dropShadowPrefab;

    public IReadOnlyList<EnemyLootItemEntry> ItemDrops => itemDrops;
    public int MinGold => minGold;
    public int MaxGold => maxGold;
    public int MinXp => minXp;
    public int MaxXp => maxXp;
    public string GoldRewardSfxId => goldRewardSfxId;
    public GameObject DropGlowPrefab => dropGlowPrefab;
    public GameObject DropShadowPrefab => dropShadowPrefab;

#if UNITY_EDITOR
    private void OnValidate()
    {
        maxGold = Mathf.Max(minGold, maxGold);
        maxXp = Mathf.Max(minXp, maxXp);

        if (itemDrops == null)
            return;

        for (int i = 0; i < itemDrops.Count; i++)
        {
            itemDrops[i]?.Validate();
        }
    }
#endif
}

[Serializable]
public class EnemyLootItemEntry
{
    [SerializeField] private InventoryItemSO item;
    [SerializeField, Range(0f, 1f)] private float dropChance = 1f;
    [SerializeField, Min(1)] private int minQuantity = 1;
    [SerializeField, Min(1)] private int maxQuantity = 1;

    public InventoryItemSO Item => item;
    public float DropChance => dropChance;
    public int MinQuantity => minQuantity;
    public int MaxQuantity => maxQuantity;

#if UNITY_EDITOR
    public void Validate()
    {
        dropChance = Mathf.Clamp01(dropChance);
        minQuantity = Mathf.Max(1, minQuantity);
        maxQuantity = Mathf.Max(minQuantity, maxQuantity);
    }
#endif
}
