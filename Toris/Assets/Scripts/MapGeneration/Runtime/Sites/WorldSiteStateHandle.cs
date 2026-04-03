using System.Collections.Generic;

public readonly struct WorldSiteStateHandle
{
    private readonly WorldSiteStateRecord record;
    private readonly HashSet<int> consumedIds;
    private readonly int spawnId;

    public WorldSiteStateHandle(
        WorldSiteStateRecord record,
        HashSet<int> consumedIds,
        int spawnId)
    {
        this.record = record;
        this.consumedIds = consumedIds;
        this.spawnId = spawnId;
    }

    public bool IsValid => record != null;

    public bool IsConsumed
    {
        get
        {
            if (record == null)
                return false;

            return record.IsConsumed || (consumedIds != null && consumedIds.Contains(spawnId));
        }
    }

    public void MarkConsumed()
    {
        if (record == null)
            return;

        record.IsConsumed = true;
        consumedIds?.Add(spawnId);
    }

    public bool GetBool(string key, bool defaultValue = false)
    {
        return record != null ? record.GetBool(key, defaultValue) : defaultValue;
    }

    public void SetBool(string key, bool value)
    {
        record?.SetBool(key, value);
    }

    public int GetInt(string key, int defaultValue = 0)
    {
        return record != null ? record.GetInt(key, defaultValue) : defaultValue;
    }

    public void SetInt(string key, int value)
    {
        record?.SetInt(key, value);
    }

    public float GetFloat(string key, float defaultValue = 0f)
    {
        return record != null ? record.GetFloat(key, defaultValue) : defaultValue;
    }

    public void SetFloat(string key, float value)
    {
        record?.SetFloat(key, value);
    }

    public string GetString(string key, string defaultValue = null)
    {
        return record != null ? record.GetString(key, defaultValue) : defaultValue;
    }

    public void SetString(string key, string value)
    {
        record?.SetString(key, value);
    }
}