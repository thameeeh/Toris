using UnityEngine;

public static class DamageNumberSettings
{
    private const string ShowDamageNumbersKey = "gameplay.damage_numbers.enabled";
    private const int DefaultShowDamageNumbers = 1;

    private static bool loaded;
    private static bool showDamageNumbers = true;

    public static bool ShowDamageNumbers
    {
        get
        {
            EnsureLoaded();
            return showDamageNumbers;
        }
    }

    public static void SetShowDamageNumbers(bool enabled)
    {
        EnsureLoaded();
        showDamageNumbers = enabled;
        PlayerPrefs.SetInt(ShowDamageNumbersKey, enabled ? 1 : 0);
    }

    public static void Load()
    {
        showDamageNumbers = PlayerPrefs.GetInt(ShowDamageNumbersKey, DefaultShowDamageNumbers) == 1;
        loaded = true;
    }

    public static void Save()
    {
        PlayerPrefs.Save();
    }

    private static void EnsureLoaded()
    {
        if (!loaded)
        {
            Load();
        }
    }
}
