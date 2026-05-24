using UnityEngine;

public static class AudioVolumeSettings
{
    private const string MasterVolumeKey = "audio.volume.master";
    private const string MusicVolumeKey = "audio.volume.music";
    private const string SfxVolumeKey = "audio.volume.sfx";
    private const float DefaultMasterVolume = 1f;
    private const float DefaultMusicVolume = 0.8f;
    private const float DefaultSfxVolume = 1f;

    private static bool loaded;
    private static float masterVolume = DefaultMasterVolume;
    private static float musicVolume = DefaultMusicVolume;
    private static float sfxVolume = DefaultSfxVolume;

    public static float MasterVolume
    {
        get
        {
            EnsureLoaded();
            return masterVolume;
        }
    }

    public static float MusicVolume
    {
        get
        {
            EnsureLoaded();
            return musicVolume;
        }
    }

    public static float SfxVolume
    {
        get
        {
            EnsureLoaded();
            return sfxVolume;
        }
    }

    public static float EffectiveMusicVolume => MusicVolume;
    public static float EffectiveSfxVolume => SfxVolume;

    public static void Load()
    {
        masterVolume = ReadVolume(MasterVolumeKey, DefaultMasterVolume);
        musicVolume = ReadVolume(MusicVolumeKey, DefaultMusicVolume);
        sfxVolume = ReadVolume(SfxVolumeKey, DefaultSfxVolume);
        loaded = true;
        ApplyMasterVolume();
    }

    public static void SetMasterVolume(float value)
    {
        EnsureLoaded();
        masterVolume = SaveVolume(MasterVolumeKey, value);
        ApplyMasterVolume();
    }

    public static void SetMusicVolume(float value)
    {
        EnsureLoaded();
        musicVolume = SaveVolume(MusicVolumeKey, value);
    }

    public static void SetSfxVolume(float value)
    {
        EnsureLoaded();
        sfxVolume = SaveVolume(SfxVolumeKey, value);
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

    private static float ReadVolume(string key, float defaultValue)
    {
        return Mathf.Clamp01(PlayerPrefs.GetFloat(key, defaultValue));
    }

    private static float SaveVolume(string key, float value)
    {
        float clampedValue = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(key, clampedValue);
        return clampedValue;
    }

    private static void ApplyMasterVolume()
    {
        AudioListener.volume = masterVolume;
    }
}
