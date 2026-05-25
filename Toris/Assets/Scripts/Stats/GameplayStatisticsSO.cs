using UnityEngine;
using System.Collections.Generic;
using OutlandHaven.SaveSystem;
using OutlandHaven.Inventory;

/// <summary>
/// Persistent gameplay statistics tracker.
/// Subscribes to the shared QuestFact event bus and accumulates lifetime counters.
/// Lives as a singleton ScriptableObject asset in Resources/GameData/.
///
/// Save/Load: The SaveDataOrchestrator calls CaptureToSaveData() and RestoreFromSaveData()
/// during the standard Toris save pipeline. ScriptableObject field values are volatile
/// in standalone builds, so all persistence goes through the JSON save files.
/// </summary>
[CreateAssetMenu(fileName = "GameplayStatistics", menuName = "Stats/Gameplay Statistics")]
public class GameplayStatisticsSO : ScriptableObject
{
    private const string DefaultResourcePath = "GameData/GameplayStatistics";

    [Header("Lifetime Kill Stats")]
    [Tooltip("Total number of enemies killed across all types.")]
    public int TotalKills;

    [Tooltip("Total number of wolves killed specifically.")]
    public int WolfKills;

    [Header("Lifetime Pick Up Stats")]
    [Tooltip("Total number of items picked up across all types.")]
    public int TotalPickUps;

    [Header("Lifetime Time Stats")]
    [Tooltip("Total playtime accumulated in seconds across all sessions.")]
    public float PlayTime;

    [Header("Lifetime Generic Item Pick Up Stats")]
    [Tooltip("Pickup counts for each unique item ID. Keys match the InventoryItemSO asset name.")]
    public Dictionary<string, int> ItemPickUps = new Dictionary<string, int>(System.StringComparer.Ordinal);

    private void OnEnable()
    {
        PixelCrushersQuestFactReporter.FactReported += OnFactReported;
    }

    private void OnDisable()
    {
        PixelCrushersQuestFactReporter.FactReported -= OnFactReported;
    }

    private void OnFactReported(QuestFact fact)
    {
        switch (fact.Type)
        {
            case QuestFactType.Kill:
                TotalKills += fact.Amount;

                // Track wolf kills specifically by checking ExactId or TypeOrTag
                if (!string.IsNullOrEmpty(fact.ExactId) &&
                    fact.ExactId.IndexOf("Wolf", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    WolfKills += fact.Amount;
                }
                else if (!string.IsNullOrEmpty(fact.TypeOrTag) &&
                         fact.TypeOrTag.IndexOf("Wolf", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    WolfKills += fact.Amount;
                }
                break;

            case QuestFactType.PickUp:
                TotalPickUps += fact.Amount;

                // Generically track all item pickups by item ID (fact.ExactId)
                if (!string.IsNullOrEmpty(fact.ExactId))
                {
                    if (ItemPickUps == null)
                    {
                        ItemPickUps = new Dictionary<string, int>(System.StringComparer.Ordinal);
                    }

                    if (ItemPickUps.ContainsKey(fact.ExactId))
                    {
                        ItemPickUps[fact.ExactId] += fact.Amount;
                    }
                    else
                    {
                        ItemPickUps[fact.ExactId] = fact.Amount;
                    }
                }
                break;
        }
    }

    /// <summary>
    /// Gets the pickup count for a specific item ID.
    /// </summary>
    public int GetPickUpCount(string itemAssetId)
    {
        if (ItemPickUps != null && !string.IsNullOrEmpty(itemAssetId) && ItemPickUps.TryGetValue(itemAssetId, out int count))
        {
            return count;
        }
        return 0;
    }

    /// <summary>
    /// Gets the pickup count for a specific InventoryItemSO blueprint.
    /// </summary>
    public int GetPickUpCount(InventoryItemSO item)
    {
        if (item == null) return 0;
        return GetPickUpCount(item.name);
    }

    /// <summary>
    /// Resolves item details from the Master Item Database and returns a breakdown of all item pickups.
    /// </summary>
    public Dictionary<InventoryItemSO, int> GetResolvedItemPickUps(ItemDatabaseSO itemDatabase)
    {
        var resolved = new Dictionary<InventoryItemSO, int>();
        if (itemDatabase == null || ItemPickUps == null)
            return resolved;

        itemDatabase.Initialize();
        foreach (var kvp in ItemPickUps)
        {
            InventoryItemSO blueprint = itemDatabase.GetItemByID(kvp.Key);
            if (blueprint != null)
            {
                resolved[blueprint] = kvp.Value;
            }
        }
        return resolved;
    }

    /// <summary>
    /// Captures current stats into a serializable DTO for the save system.
    /// </summary>
    public SavedGameplayStatisticsData CaptureToSaveData()
    {
        return new SavedGameplayStatisticsData
        {
            TotalKills = TotalKills,
            WolfKills = WolfKills,
            PlayTime = PlayTime,
            TotalPickUps = TotalPickUps,
            ItemPickUps = ItemPickUps != null ? new Dictionary<string, int>(ItemPickUps, System.StringComparer.Ordinal) : new Dictionary<string, int>(System.StringComparer.Ordinal)
        };
    }

    /// <summary>
    /// Restores stats from a previously saved DTO.
    /// </summary>
    public void RestoreFromSaveData(SavedGameplayStatisticsData data)
    {
        if (data == null)
        {
            ResetStats();
            return;
        }

        TotalKills = data.TotalKills;
        WolfKills = data.WolfKills;
        PlayTime = data.PlayTime;
        TotalPickUps = data.TotalPickUps;
        ItemPickUps = data.ItemPickUps != null ? new Dictionary<string, int>(data.ItemPickUps, System.StringComparer.Ordinal) : new Dictionary<string, int>(System.StringComparer.Ordinal);
    }

    /// <summary>
    /// Resets all statistics to zero. Called on new game or session clear.
    /// </summary>
    public void ResetStats()
    {
        TotalKills = 0;
        WolfKills = 0;
        PlayTime = 0f;
        TotalPickUps = 0;
        if (ItemPickUps != null)
        {
            ItemPickUps.Clear();
        }
        else
        {
            ItemPickUps = new Dictionary<string, int>(System.StringComparer.Ordinal);
        }
    }

    /// <summary>
    /// Loads the default singleton instance from Resources.
    /// </summary>
    public static GameplayStatisticsSO LoadDefault()
    {
        return Resources.Load<GameplayStatisticsSO>(DefaultResourcePath);
    }
}
