using System.Collections.Generic;

public sealed class WorldSiteStateRecord
{
    private readonly Dictionary<string, bool> boolValues = new Dictionary<string, bool>();
    private readonly Dictionary<string, int> intValues = new Dictionary<string, int>();
    private readonly Dictionary<string, float> floatValues = new Dictionary<string, float>();
    private readonly Dictionary<string, string> stringValues = new Dictionary<string, string>();

    public bool IsConsumed { get; set; }

    public bool GetBool(string key, bool defaultValue = false)
    {
        return boolValues.TryGetValue(key, out bool value) ? value : defaultValue;
    }

    public void SetBool(string key, bool value)
    {
        boolValues[key] = value;
    }

    public int GetInt(string key, int defaultValue = 0)
    {
        return intValues.TryGetValue(key, out int value) ? value : defaultValue;
    }

    public void SetInt(string key, int value)
    {
        intValues[key] = value;
    }

    public float GetFloat(string key, float defaultValue = 0f)
    {
        return floatValues.TryGetValue(key, out float value) ? value : defaultValue;
    }

    public void SetFloat(string key, float value)
    {
        floatValues[key] = value;
    }

    public string GetString(string key, string defaultValue = null)
    {
        return stringValues.TryGetValue(key, out string value) ? value : defaultValue;
    }

    public void SetString(string key, string value)
    {
        stringValues[key] = value;
    }
}