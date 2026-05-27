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
[CreateAssetMenu(fileName = "GameplayStatistics", menuName = "Outland Haven/System/Gameplay Statistics")]
public class GameplayStatisticsSO : ScriptableObject
{
    private const string DefaultResourcePath = "GameData/GameplayStatistics";

    // --- Encapsulated runtime state ---
    // Public getters for read access; mutations go through dedicated methods
    // to prevent accidental writes from external scripts.

    [Header("Lifetime Kill Stats")]
    [Tooltip("Total number of enemies killed across all types.")]
    [SerializeField] private int _totalKills;
    public int TotalKills => _totalKills;

    [Tooltip("Total number of wolves killed specifically.")]
    [SerializeField] private int _wolfKills;
    public int WolfKills => _wolfKills;

    [Header("Lifetime Pick Up Stats")]
    [Tooltip("Total number of items picked up across all types.")]
    [SerializeField] private int _totalPickUps;
    public int TotalPickUps => _totalPickUps;

    [Header("Lifetime Time Stats")]
    [Tooltip("Total playtime accumulated in seconds across all sessions.")]
    [SerializeField] private float _playTime;
    public float PlayTime => _playTime;

    [Header("Lifetime Generic Item Pick Up Stats")]
    [Tooltip("Pickup counts for each unique item ID. Keys match the InventoryItemSO asset name.")]
    private Dictionary<string, int> _itemPickUps = new Dictionary<string, int>(System.StringComparer.Ordinal);

    /// <summary>
    /// Read-only view of per-item pickup counts. Returns null-safe.
    /// </summary>
    public IReadOnlyDictionary<string, int> ItemPickUps => _itemPickUps;

    // --- Mutation API ---

    /// <summary>
    /// Adds elapsed time to the play timer. Called by SaveManager.Update() each frame.
    /// </summary>
    public void AddPlayTime(float deltaSeconds)
    {
        _playTime += deltaSeconds;
    }

    // --- Event-driven stat tracking (internal) ---

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
                _totalKills += fact.Amount;

                // Track wolf kills specifically by checking ExactId or TypeOrTag
                if (!string.IsNullOrEmpty(fact.ExactId) &&
                    fact.ExactId.IndexOf("Wolf", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _wolfKills += fact.Amount;
                }
                else if (!string.IsNullOrEmpty(fact.TypeOrTag) &&
                         fact.TypeOrTag.IndexOf("Wolf", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _wolfKills += fact.Amount;
                }
                break;

            case QuestFactType.PickUp:
                _totalPickUps += fact.Amount;

                // Generically track all item pickups by item ID (fact.ExactId)
                if (!string.IsNullOrEmpty(fact.ExactId))
                {
                    if (_itemPickUps == null)
                    {
                        _itemPickUps = new Dictionary<string, int>(System.StringComparer.Ordinal);
                    }

                    if (_itemPickUps.ContainsKey(fact.ExactId))
                    {
                        _itemPickUps[fact.ExactId] += fact.Amount;
                    }
                    else
                    {
                        _itemPickUps[fact.ExactId] = fact.Amount;
                    }
                }
                break;
        }
    }

    // --- Query API ---

    /// <summary>
    /// Gets the pickup count for a specific item ID.
    /// </summary>
    public int GetPickUpCount(string itemAssetId)
    {
        if (_itemPickUps != null && !string.IsNullOrEmpty(itemAssetId) && _itemPickUps.TryGetValue(itemAssetId, out int count))
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
        if (itemDatabase == null || _itemPickUps == null)
            return resolved;

        itemDatabase.Initialize();
        foreach (var kvp in _itemPickUps)
        {
            InventoryItemSO blueprint = itemDatabase.GetItemByID(kvp.Key);
            if (blueprint != null)
            {
                resolved[blueprint] = kvp.Value;
            }
        }
        return resolved;
    }

    // --- Save/Load pipeline ---

    /// <summary>
    /// Captures current stats into a serializable DTO for the save system.
    /// </summary>
    public SavedGameplayStatisticsData CaptureToSaveData()
    {
        return new SavedGameplayStatisticsData
        {
            TotalKills = _totalKills,
            WolfKills = _wolfKills,
            PlayTime = _playTime,
            TotalPickUps = _totalPickUps,
            ItemPickUps = _itemPickUps != null ? new Dictionary<string, int>(_itemPickUps, System.StringComparer.Ordinal) : new Dictionary<string, int>(System.StringComparer.Ordinal)
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

        _totalKills = data.TotalKills;
        _wolfKills = data.WolfKills;
        _playTime = data.PlayTime;
        _totalPickUps = data.TotalPickUps;
        _itemPickUps = data.ItemPickUps != null ? new Dictionary<string, int>(data.ItemPickUps, System.StringComparer.Ordinal) : new Dictionary<string, int>(System.StringComparer.Ordinal);
    }

    /// <summary>
    /// Resets all statistics to zero. Called on new game or session clear.
    /// </summary>
    public void ResetStats()
    {
        _totalKills = 0;
        _wolfKills = 0;
        _playTime = 0f;
        _totalPickUps = 0;
        if (_itemPickUps != null)
        {
            _itemPickUps.Clear();
        }
        else
        {
            _itemPickUps = new Dictionary<string, int>(System.StringComparer.Ordinal);
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
