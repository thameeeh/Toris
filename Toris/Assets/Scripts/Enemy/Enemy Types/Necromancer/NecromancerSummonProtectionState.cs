using UnityEngine;

public sealed class NecromancerSummonProtectionState
{
    public int ActiveBloodMageCount { get; private set; }
    public bool IsAwaitingSummonedBloodMages { get; private set; }
    public bool HasActiveProtection => ActiveBloodMageCount > 0;

    public void MarkPending()
    {
        IsAwaitingSummonedBloodMages = true;
    }

    public void RegisterBloodMage()
    {
        ActiveBloodMageCount++;
        IsAwaitingSummonedBloodMages = false;
    }

    public void UnregisterBloodMage()
    {
        ActiveBloodMageCount = Mathf.Max(0, ActiveBloodMageCount - 1);

        if (ActiveBloodMageCount == 0)
            IsAwaitingSummonedBloodMages = false;
    }

    public void Reset()
    {
        ActiveBloodMageCount = 0;
        IsAwaitingSummonedBloodMages = false;
    }
}
