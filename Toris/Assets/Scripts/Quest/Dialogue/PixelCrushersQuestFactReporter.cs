using System;
using UnityEngine;

/// <summary>
/// Single Toris entry point for gameplay facts that may affect Pixel Crushers quests.
/// Gameplay systems report facts here; quest-specific progress rules are handled by listeners.
/// </summary>
public static class PixelCrushersQuestFactReporter
{
    public static event Action<QuestFact> FactReported;

    public static void Report(QuestFact fact)
    {
        if (fact.Type == QuestFactType.None)
            return;

        if (fact.Amount <= 0)
            fact.Amount = 1;

#if UNITY_EDITOR
        Debug.Log(
            $"[PixelCrushersQuestFactReporter] Fact type={fact.Type}, exactId='{fact.ExactId}', typeOrTag='{fact.TypeOrTag}', amount={fact.Amount}, context='{fact.ContextId}'.");
#endif

        FactReported?.Invoke(fact);
    }
}
