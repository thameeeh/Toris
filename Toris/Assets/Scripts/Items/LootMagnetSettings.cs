using UnityEngine;

/// <summary>
/// Static configuration manager for Gameplay settings, specifically the Loot Magnet / Vacuum feature.
/// Persists the enabled/disabled state using Unity PlayerPrefs.
/// </summary>
public static class LootMagnetSettings
{
    private const string LootMagnetEnabledKey = "gameplay.loot_magnet.enabled";
    private const int DefaultLootMagnetEnabled = 1; // 1 = true, 0 = false

    private static bool loaded;
    private static bool lootMagnetEnabled = true;

    public static bool LootMagnetEnabled
    {
        get
        {
            EnsureLoaded();
            return lootMagnetEnabled;
        }
    }

    public static void SetLootMagnetEnabled(bool enabled)
    {
        EnsureLoaded();
        lootMagnetEnabled = enabled;
        PlayerPrefs.SetInt(LootMagnetEnabledKey, enabled ? 1 : 0);
    }

    public static void Load()
    {
        lootMagnetEnabled = PlayerPrefs.GetInt(LootMagnetEnabledKey, DefaultLootMagnetEnabled) == 1;
        loaded = true;
    }

    public static void Save()
    {
        PlayerPrefs.Save();
    }

    private static void EnsureLoaded()
    {
        if (!loaded)
            Load();
    }
}
