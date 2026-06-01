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

public struct GameDisplayOption : IEquatable<GameDisplayOption>
{
    public GameDisplayOption(int index, string name, int width, int height)
    {
        Index = Mathf.Max(0, index);
        Name = string.IsNullOrWhiteSpace(name) ? $"Display {Index + 1}" : name.Trim();
        Width = Mathf.Max(1, width);
        Height = Mathf.Max(1, height);
    }

    public int Index { get; }
    public string Name { get; }
    public int Width { get; }
    public int Height { get; }
    public GameDisplayResolution Resolution => new GameDisplayResolution(Width, Height);

    public bool Equals(GameDisplayOption other)
    {
        return Index == other.Index
            && Width == other.Width
            && Height == other.Height
            && string.Equals(Name, other.Name, StringComparison.Ordinal);
    }

    public override bool Equals(object obj)
    {
        return obj is GameDisplayOption other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hashCode = Index;
            hashCode = (hashCode * 397) ^ Width;
            hashCode = (hashCode * 397) ^ Height;
            hashCode = (hashCode * 397) ^ (Name != null ? Name.GetHashCode() : 0);
            return hashCode;
        }
    }

    public override string ToString()
    {
        return $"Display {Index + 1} - {Width} x {Height}";
    }
}

public struct GameDisplaySettingsSnapshot : IEquatable<GameDisplaySettingsSnapshot>
{
    public GameDisplaySettingsSnapshot(int width, int height, FullScreenMode windowMode, int displayIndex = 0)
    {
        Width = Mathf.Max(1, width);
        Height = Mathf.Max(1, height);
        WindowMode = GameDisplaySettings.ResolveSupportedWindowMode(windowMode);
        DisplayIndex = Mathf.Max(0, displayIndex);
    }

    public int Width { get; }
    public int Height { get; }
    public FullScreenMode WindowMode { get; }
    public int DisplayIndex { get; }
    public GameDisplayResolution Resolution => new GameDisplayResolution(Width, Height);

    public bool Equals(GameDisplaySettingsSnapshot other)
    {
        return Width == other.Width
            && Height == other.Height
            && WindowMode == other.WindowMode
            && DisplayIndex == other.DisplayIndex;
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
            hashCode = (hashCode * 397) ^ DisplayIndex;
            return hashCode;
        }
    }
}

public static class GameDisplaySettings
{
    private const string ResolutionWidthKey = "display.resolution.width";
    private const string ResolutionHeightKey = "display.resolution.height";
    private const string WindowModeKey = "display.window_mode";
    private const string DisplayIndexKey = "display.index";
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
                Screen.fullScreenMode,
                ResolveCurrentDisplayIndex());
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
        int defaultDisplayIndex = ResolveCurrentDisplayIndex();
        bool hasSavedSettings = PlayerPrefs.HasKey(ResolutionWidthKey)
            || PlayerPrefs.HasKey(ResolutionHeightKey)
            || PlayerPrefs.HasKey(WindowModeKey)
            || PlayerPrefs.HasKey(DisplayIndexKey);

        int width = PlayerPrefs.GetInt(ResolutionWidthKey, defaultWidth);
        int height = PlayerPrefs.GetInt(ResolutionHeightKey, defaultHeight);
        FullScreenMode windowMode = ResolveWindowMode(PlayerPrefs.GetInt(WindowModeKey, (int)defaultMode));
        int displayIndex = PlayerPrefs.GetInt(DisplayIndexKey, defaultDisplayIndex);

        savedSettings = Sanitize(new GameDisplaySettingsSnapshot(width, height, windowMode, displayIndex));
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
        PlayerPrefs.SetInt(DisplayIndexKey, savedSettings.DisplayIndex);
        PlayerPrefs.Save();
    }

    public static void Apply(GameDisplaySettingsSnapshot settings)
    {
        GameDisplaySettingsSnapshot sanitizedSettings = Sanitize(settings);
        MoveMainWindowToDisplayIfChanged(sanitizedSettings);
        ApplyResolutionAndMode(sanitizedSettings);
    }

    public static AsyncOperation MoveMainWindowToDisplayIfChanged(GameDisplaySettingsSnapshot settings)
    {
        GameDisplaySettingsSnapshot sanitizedSettings = Sanitize(settings);
        // Keep the window where the player placed it unless the selected monitor changed.
        return sanitizedSettings.DisplayIndex == ResolveCurrentDisplayIndex()
            ? null
            : MoveMainWindowToDisplay(sanitizedSettings);
    }

    public static AsyncOperation MoveMainWindowToDisplay(GameDisplaySettingsSnapshot settings)
    {
        GameDisplaySettingsSnapshot sanitizedSettings = Sanitize(settings);
        List<DisplayInfo> displays = GetDisplayLayoutSafe();
        if (displays.Count == 0)
        {
            return null;
        }

        int displayIndex = ClampDisplayIndex(sanitizedSettings.DisplayIndex, displays.Count);
        DisplayInfo display = displays[displayIndex];
        Vector2Int centeredPosition = ResolveCenteredWindowPosition(
            display,
            ResolveOutputResolution(sanitizedSettings));

        try
        {
            return Screen.MoveMainWindowTo(display, centeredPosition);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static void ApplyResolutionAndMode(GameDisplaySettingsSnapshot settings)
    {
        GameDisplaySettingsSnapshot sanitizedSettings = Sanitize(settings);
        GameDisplayResolution resolution = ResolveOutputResolution(sanitizedSettings);

        Screen.SetResolution(
            resolution.Width,
            resolution.Height,
            sanitizedSettings.WindowMode);
    }

    public static GameDisplaySettingsSnapshot Normalize(GameDisplaySettingsSnapshot settings)
    {
        return Sanitize(settings);
    }

    public static List<GameDisplayOption> GetAvailableDisplays()
    {
        List<GameDisplayOption> displayOptions = BuildDisplayOptions();
        if (displayOptions.Count == 0)
        {
            GameDisplayResolution fallbackResolution = ResolveCurrentDisplayResolution();
            displayOptions.Add(new GameDisplayOption(0, "Current Display", fallbackResolution.Width, fallbackResolution.Height));
        }

        return displayOptions;
    }

    public static List<GameDisplayResolution> GetAvailableResolutions(int displayIndex)
    {
        EnsureLoaded();
        List<GameDisplayResolution> resolutions = GetAvailableResolutionsWithoutLoad(displayIndex);
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
        int displayIndex = ResolveValidDisplayIndex(settings.DisplayIndex);
        GameDisplayResolution resolution = ResolvePreferredResolution(settings.Resolution, displayIndex);
        return new GameDisplaySettingsSnapshot(
            resolution.Width,
            resolution.Height,
            ResolveSupportedWindowMode(settings.WindowMode),
            displayIndex);
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

    private static GameDisplayResolution ResolvePreferredResolution(GameDisplayResolution requestedResolution, int displayIndex)
    {
        List<GameDisplayResolution> availableResolutions = GetAvailableResolutionsWithoutLoad(displayIndex);
        if (availableResolutions.Count == 0)
        {
            return ResolveDisplayResolution(displayIndex);
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

    private static List<GameDisplayResolution> GetAvailableResolutionsWithoutLoad(int displayIndex)
    {
        List<GameDisplayResolution> resolutions = new List<GameDisplayResolution>();
        GameDisplayResolution nativeResolution = ResolveDisplayResolution(displayIndex);
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

    private static GameDisplayResolution ResolveOutputResolution(GameDisplaySettingsSnapshot settings)
    {
        return settings.WindowMode == FullScreenMode.Windowed
            ? settings.Resolution
            : ResolveDisplayResolution(settings.DisplayIndex);
    }

    private static Vector2Int ResolveCenteredWindowPosition(DisplayInfo display, GameDisplayResolution resolution)
    {
        int centeredX = Mathf.Max(0, (display.width - resolution.Width) / 2);
        int centeredY = Mathf.Max(0, (display.height - resolution.Height) / 2);
        return new Vector2Int(centeredX, centeredY);
    }

    private static GameDisplayResolution ResolveDisplayResolution(int displayIndex)
    {
        List<GameDisplayOption> displayOptions = BuildDisplayOptions();
        if (displayOptions.Count == 0)
        {
            return ResolveCurrentDisplayResolution();
        }

        return ResolveDisplayOption(displayIndex, displayOptions).Resolution;
    }

    private static GameDisplayResolution ResolveCurrentDisplayResolution()
    {
        int width = Screen.currentResolution.width > 0
            ? Screen.currentResolution.width
            : ResolveDefaultWidth();

        int height = Screen.currentResolution.height > 0
            ? Screen.currentResolution.height
            : ResolveDefaultHeight();

        return new GameDisplayResolution(width, height);
    }

    private static int ResolveValidDisplayIndex(int displayIndex)
    {
        List<GameDisplayOption> displayOptions = BuildDisplayOptions();
        return displayOptions.Count == 0
            ? 0
            : ResolveDisplayOption(displayIndex, displayOptions).Index;
    }

    private static GameDisplayOption ResolveDisplayOption(int displayIndex, List<GameDisplayOption> displayOptions)
    {
        for (int i = 0; i < displayOptions.Count; i++)
        {
            if (displayOptions[i].Index == displayIndex)
            {
                return displayOptions[i];
            }
        }

        return displayOptions[ClampDisplayIndex(displayIndex, displayOptions.Count)];
    }

    private static int ResolveCurrentDisplayIndex()
    {
        List<DisplayInfo> displays = GetDisplayLayoutSafe();
        if (displays.Count == 0)
        {
            return 0;
        }

        try
        {
            DisplayInfo currentDisplay = Screen.mainWindowDisplayInfo;
            for (int i = 0; i < displays.Count; i++)
            {
                DisplayInfo display = displays[i];
                if (display.width == currentDisplay.width
                    && display.height == currentDisplay.height
                    && string.Equals(display.name, currentDisplay.name, StringComparison.Ordinal))
                {
                    return i;
                }
            }
        }
        catch (Exception)
        {
            return 0;
        }

        return 0;
    }

    private static List<GameDisplayOption> BuildDisplayOptions()
    {
        List<DisplayInfo> displays = GetDisplayLayoutSafe();
        List<GameDisplayOption> displayOptions = new List<GameDisplayOption>(displays.Count);

        for (int i = 0; i < displays.Count; i++)
        {
            DisplayInfo display = displays[i];
            if (display.width < MinimumResolutionDimension || display.height < MinimumResolutionDimension)
            {
                continue;
            }

            displayOptions.Add(new GameDisplayOption(i, display.name, display.width, display.height));
        }

        return displayOptions;
    }

    private static List<DisplayInfo> GetDisplayLayoutSafe()
    {
        List<DisplayInfo> displays = new List<DisplayInfo>();
        try
        {
            Screen.GetDisplayLayout(displays);
        }
        catch (Exception)
        {
            displays.Clear();
        }

        return displays;
    }

    private static int ClampDisplayIndex(int displayIndex, int displayCount)
    {
        if (displayCount <= 0)
        {
            return 0;
        }

        return Mathf.Clamp(displayIndex, 0, displayCount - 1);
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
