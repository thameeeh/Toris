using UnityEngine;
using OutlandHaven.SaveSystem;

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
        }
    }

    /// <summary>
    /// Captures current stats into a serializable DTO for the save system.
    /// </summary>
    public SavedGameplayStatisticsData CaptureToSaveData()
    {
        return new SavedGameplayStatisticsData
        {
            TotalKills = TotalKills,
            WolfKills = WolfKills
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
    }

    /// <summary>
    /// Resets all statistics to zero. Called on new game or session clear.
    /// </summary>
    public void ResetStats()
    {
        TotalKills = 0;
        WolfKills = 0;
    }

    /// <summary>
    /// Loads the default singleton instance from Resources.
    /// </summary>
    public static GameplayStatisticsSO LoadDefault()
    {
        return Resources.Load<GameplayStatisticsSO>(DefaultResourcePath);
    }
}
