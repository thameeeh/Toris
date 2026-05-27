using System;
using UnityEngine;

public static class FpsDisplaySettings
{
    private const string ShowFpsKey = "ui.show_fps.enabled";
    private const int DefaultShowFps = 0;

    private static bool loaded;
    private static bool showFps;

    public static event Action<bool> OnShowFpsChanged;

    public static bool ShowFps
    {
        get
        {
            EnsureLoaded();
            return showFps;
        }
    }

    public static void SetShowFps(bool enabled)
    {
        EnsureLoaded();
        if (showFps == enabled)
        {
            return;
        }

        showFps = enabled;
        PlayerPrefs.SetInt(ShowFpsKey, enabled ? 1 : 0);
        OnShowFpsChanged?.Invoke(showFps);
    }

    public static void Load()
    {
        showFps = PlayerPrefs.GetInt(ShowFpsKey, DefaultShowFps) == 1;
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
