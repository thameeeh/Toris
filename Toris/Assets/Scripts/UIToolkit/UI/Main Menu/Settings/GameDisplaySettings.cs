using System;
using System.Collections.Generic;
using UnityEngine;

public struct GameDisplayResolution : IEquatable<GameDisplayResolution>
{
    public GameDisplayResolution(int width, int height)
    {
        Width = Mathf.Max(1, width);
        Height = Mathf.Max(1, height);
    }

    public int Width { get; }
    public int Height { get; }

    public bool Equals(GameDisplayResolution other)
    {
        return Width == other.Width && Height == other.Height;
    }

    public override bool Equals(object obj)
    {
        return obj is GameDisplayResolution other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (Width * 397) ^ Height;
        }
    }

    public override string ToString()
    {
        return $"{Width} x {Height}";
    }
}

public struct GameDisplaySettingsSnapshot : IEquatable<GameDisplaySettingsSnapshot>
{
    public GameDisplaySettingsSnapshot(int width, int height, FullScreenMode windowMode)
    {
        Width = Mathf.Max(1, width);
        Height = Mathf.Max(1, height);
        WindowMode = GameDisplaySettings.ResolveSupportedWindowMode(windowMode);
    }

    public int Width { get; }
    public int Height { get; }
    public FullScreenMode WindowMode { get; }
    public GameDisplayResolution Resolution => new GameDisplayResolution(Width, Height);

    public bool Equals(GameDisplaySettingsSnapshot other)
    {
        return Width == other.Width
            && Height == other.Height
            && WindowMode == other.WindowMode;
    }

    public override bool Equals(object obj)
    {
        return obj is GameDisplaySettingsSnapshot other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hashCode = Width;
            hashCode = (hashCode * 397) ^ Height;
            hashCode = (hashCode * 397) ^ (int)WindowMode;
            return hashCode;
        }
    }
}

public static class GameDisplaySettings
{
    private const string ResolutionWidthKey = "display.resolution.width";
    private const string ResolutionHeightKey = "display.resolution.height";
    private const string WindowModeKey = "display.window_mode";
    private const int MinimumResolutionDimension = 1;

    private static readonly GameDisplayResolution[] PreferredResolutions =
    {
        new GameDisplayResolution(1280, 720),
        new GameDisplayResolution(1366, 768),
        new GameDisplayResolution(1600, 900),
        new GameDisplayResolution(1920, 1080),
        new GameDisplayResolution(2560, 1440)
    };

    private static bool loaded;
    private static GameDisplaySettingsSnapshot savedSettings;

    public static GameDisplaySettingsSnapshot SavedSettings
    {
        get
        {
            EnsureLoaded();
            return savedSettings;
        }
    }

    public static GameDisplaySettingsSnapshot CurrentSettings
    {
        get
        {
            return new GameDisplaySettingsSnapshot(
                Mathf.Max(MinimumResolutionDimension, Screen.width),
                Mathf.Max(MinimumResolutionDimension, Screen.height),
                Screen.fullScreenMode);
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeBeforeSceneLoad()
    {
        Load();
    }

    public static void Load()
    {
        int defaultWidth = ResolveDefaultWidth();
        int defaultHeight = ResolveDefaultHeight();
        FullScreenMode defaultMode = ResolveSupportedWindowMode(Screen.fullScreenMode);
        bool hasSavedSettings = PlayerPrefs.HasKey(ResolutionWidthKey)
            || PlayerPrefs.HasKey(ResolutionHeightKey)
            || PlayerPrefs.HasKey(WindowModeKey);

        int width = PlayerPrefs.GetInt(ResolutionWidthKey, defaultWidth);
        int height = PlayerPrefs.GetInt(ResolutionHeightKey, defaultHeight);
        FullScreenMode windowMode = ResolveWindowMode(PlayerPrefs.GetInt(WindowModeKey, (int)defaultMode));

        savedSettings = Sanitize(new GameDisplaySettingsSnapshot(width, height, windowMode));
        loaded = true;

        if (hasSavedSettings)
        {
            Apply(savedSettings);
        }
    }

    public static void Save(GameDisplaySettingsSnapshot settings)
    {
        EnsureLoaded();

        savedSettings = Sanitize(settings);
        PlayerPrefs.SetInt(ResolutionWidthKey, savedSettings.Width);
        PlayerPrefs.SetInt(ResolutionHeightKey, savedSettings.Height);
        PlayerPrefs.SetInt(WindowModeKey, (int)savedSettings.WindowMode);
        PlayerPrefs.Save();
    }

    public static void Apply(GameDisplaySettingsSnapshot settings)
    {
        GameDisplaySettingsSnapshot sanitizedSettings = Sanitize(settings);
        GameDisplayResolution resolution = sanitizedSettings.WindowMode == FullScreenMode.Windowed
            ? sanitizedSettings.Resolution
            : ResolveNativeDisplayResolution();

        Screen.SetResolution(
            resolution.Width,
            resolution.Height,
            sanitizedSettings.WindowMode);
    }

    public static List<GameDisplayResolution> GetAvailableResolutions()
    {
        EnsureLoaded();

        List<GameDisplayResolution> resolutions = new List<GameDisplayResolution>();
        GameDisplayResolution nativeResolution = ResolveNativeDisplayResolution();
        for (int i = 0; i < PreferredResolutions.Length; i++)
        {
            GameDisplayResolution resolution = PreferredResolutions[i];
            if (FitsNativeDisplay(resolution, nativeResolution))
            {
                resolutions.Add(resolution);
            }
        }

        if (resolutions.Count == 0)
        {
            resolutions.Add(nativeResolution);
        }

        resolutions.Sort(CompareResolutionDescending);
        return resolutions;
    }

    public static FullScreenMode ResolveSupportedWindowMode(FullScreenMode windowMode)
    {
        return windowMode == FullScreenMode.Windowed
            ? FullScreenMode.Windowed
            : FullScreenMode.FullScreenWindow;
    }

    private static void EnsureLoaded()
    {
        if (!loaded)
        {
            Load();
        }
    }

    private static GameDisplaySettingsSnapshot Sanitize(GameDisplaySettingsSnapshot settings)
    {
        GameDisplayResolution resolution = ResolvePreferredResolution(settings.Resolution);
        return new GameDisplaySettingsSnapshot(
            resolution.Width,
            resolution.Height,
            ResolveSupportedWindowMode(settings.WindowMode));
    }

    private static FullScreenMode ResolveWindowMode(int savedValue)
    {
        FullScreenMode windowMode = Enum.IsDefined(typeof(FullScreenMode), savedValue)
            ? (FullScreenMode)savedValue
            : ResolveSupportedWindowMode(Screen.fullScreenMode);

        return ResolveSupportedWindowMode(windowMode);
    }

    private static int ResolveDefaultWidth()
    {
        if (Screen.width > 0)
        {
            return Screen.width;
        }

        return Mathf.Max(MinimumResolutionDimension, Screen.currentResolution.width);
    }

    private static int ResolveDefaultHeight()
    {
        if (Screen.height > 0)
        {
            return Screen.height;
        }

        return Mathf.Max(MinimumResolutionDimension, Screen.currentResolution.height);
    }

    private static GameDisplayResolution ResolvePreferredResolution(GameDisplayResolution requestedResolution)
    {
        List<GameDisplayResolution> availableResolutions = GetAvailableResolutionsWithoutLoad();
        if (availableResolutions.Count == 0)
        {
            return ResolveNativeDisplayResolution();
        }

        GameDisplayResolution bestResolution = availableResolutions[0];
        long bestScore = long.MaxValue;

        for (int i = 0; i < availableResolutions.Count; i++)
        {
            GameDisplayResolution candidate = availableResolutions[i];
            long widthDelta = candidate.Width - requestedResolution.Width;
            long heightDelta = candidate.Height - requestedResolution.Height;
            long score = (widthDelta * widthDelta) + (heightDelta * heightDelta);

            if (score < bestScore)
            {
                bestScore = score;
                bestResolution = candidate;
            }
        }

        return bestResolution;
    }

    private static List<GameDisplayResolution> GetAvailableResolutionsWithoutLoad()
    {
        List<GameDisplayResolution> resolutions = new List<GameDisplayResolution>();
        GameDisplayResolution nativeResolution = ResolveNativeDisplayResolution();
        for (int i = 0; i < PreferredResolutions.Length; i++)
        {
            GameDisplayResolution resolution = PreferredResolutions[i];
            if (FitsNativeDisplay(resolution, nativeResolution))
            {
                resolutions.Add(resolution);
            }
        }

        if (resolutions.Count == 0)
        {
            resolutions.Add(nativeResolution);
        }

        return resolutions;
    }

    private static GameDisplayResolution ResolveNativeDisplayResolution()
    {
        int width = Screen.currentResolution.width > 0
            ? Screen.currentResolution.width
            : ResolveDefaultWidth();

        int height = Screen.currentResolution.height > 0
            ? Screen.currentResolution.height
            : ResolveDefaultHeight();

        return new GameDisplayResolution(width, height);
    }

    private static bool FitsNativeDisplay(GameDisplayResolution resolution, GameDisplayResolution nativeResolution)
    {
        return resolution.Width <= nativeResolution.Width
            && resolution.Height <= nativeResolution.Height;
    }

    private static int CompareResolutionDescending(GameDisplayResolution left, GameDisplayResolution right)
    {
        int widthComparison = right.Width.CompareTo(left.Width);
        return widthComparison != 0
            ? widthComparison
            : right.Height.CompareTo(left.Height);
    }
}
