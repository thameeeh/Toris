using System;

public readonly struct WorldEncounterPackageState
{
    private const string ActiveKeySuffix = "active";

    private readonly string packageId;
    private readonly WorldSiteStateHandle siteState;

    public WorldEncounterPackageState(string packageId, WorldSiteStateHandle siteState)
    {
        this.packageId = packageId;
        this.siteState = siteState;
    }

    public bool IsValid => !string.IsNullOrWhiteSpace(packageId) && siteState.IsValid;

    public bool IsActive
    {
        get => GetBool(ActiveKeySuffix);
        set => SetBool(ActiveKeySuffix, value);
    }

    public bool GetBool(string key, bool defaultValue = false)
    {
        return siteState.GetBool(BuildKey(key), defaultValue);
    }

    public void SetBool(string key, bool value)
    {
        siteState.SetBool(BuildKey(key), value);
    }

    public int GetInt(string key, int defaultValue = 0)
    {
        return siteState.GetInt(BuildKey(key), defaultValue);
    }

    public void SetInt(string key, int value)
    {
        siteState.SetInt(BuildKey(key), value);
    }

    public float GetFloat(string key, float defaultValue = 0f)
    {
        return siteState.GetFloat(BuildKey(key), defaultValue);
    }

    public void SetFloat(string key, float value)
    {
        siteState.SetFloat(BuildKey(key), value);
    }

    public string GetString(string key, string defaultValue = null)
    {
        return siteState.GetString(BuildKey(key), defaultValue);
    }

    public void SetString(string key, string value)
    {
        siteState.SetString(BuildKey(key), value);
    }

    private string BuildKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Encounter package state key must not be empty.", nameof(key));

        return $"{packageId}.{key}";
    }
}
