using UnityEngine;

public sealed class WorldEncounterAlertRuntime
{
    private float level;
    private float decayDelayTimer;
    private bool maxAlertTriggered;

    public float Level => level;

    public void Reset()
    {
        level = 0f;
        decayDelayTimer = 0f;
        maxAlertTriggered = false;
    }

    public void Raise(float amount, float maxLevel, float decayDelay)
    {
        level = Mathf.Min(maxLevel, level + Mathf.Max(0f, amount));
        decayDelayTimer = Mathf.Max(0f, decayDelay);
    }

    public void Tick(float deltaTime, float decayDelayRate, float maxLevel)
    {
        if (decayDelayTimer > 0f)
        {
            decayDelayTimer -= deltaTime;
        }
        else if (level > 0f)
        {
            level = Mathf.Max(0f, level - Mathf.Max(0f, decayDelayRate) * deltaTime);
        }

        if (level < maxLevel)
            maxAlertTriggered = false;
    }

    public bool TryConsumeMaxAlert(float maxLevel)
    {
        if (level < maxLevel)
            return false;

        if (maxAlertTriggered)
            return false;

        maxAlertTriggered = true;
        return true;
    }

    public void ApplyPostMaxAlertResponse(float nextLevel, float maxLevel, float decayDelay)
    {
        level = Mathf.Clamp(nextLevel, 0f, maxLevel);
        decayDelayTimer = Mathf.Max(0f, decayDelay);

        if (level < maxLevel)
            maxAlertTriggered = false;
    }
}
