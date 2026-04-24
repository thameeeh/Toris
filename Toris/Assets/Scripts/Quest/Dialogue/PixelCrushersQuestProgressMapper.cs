using System;
using PixelCrushers.DialogueSystem;
using UnityEngine;

/// <summary>
/// Maps shared Toris gameplay facts into Pixel Crushers quest variables and states.
/// Keep quest progress rules here instead of spreading quest logic across gameplay scripts.
/// </summary>
public class PixelCrushersQuestProgressMapper : MonoBehaviour
{
    [SerializeField] private QuestFactProgressRule[] _rules = Array.Empty<QuestFactProgressRule>();

#if UNITY_EDITOR
    [Header("Debug")]
    [SerializeField] private bool _debugMapping = true;
#endif

    private void OnEnable()
    {
        PixelCrushersQuestFactReporter.FactReported += HandleFactReported;
    }

    private void OnDisable()
    {
        PixelCrushersQuestFactReporter.FactReported -= HandleFactReported;
    }

    private void HandleFactReported(QuestFact fact)
    {
        if (!PixelCrushersQuestBridge.HasDialogueManager || _rules == null)
            return;

        for (int i = 0; i < _rules.Length; i++)
        {
            QuestFactProgressRule rule = _rules[i];
            if (rule == null || !rule.Matches(fact))
                continue;

            ApplyRule(rule, fact);
        }
    }

    private void ApplyRule(QuestFactProgressRule rule, QuestFact fact)
    {
        if (rule.RequireQuestActive && PixelCrushersQuestBridge.GetQuestState(rule.QuestName) != QuestState.Active)
            return;

        int nextValue = PixelCrushersQuestBridge.IncrementIntVariable(rule.ProgressVariableName, fact.Amount);
        int clampedValue = Mathf.Min(nextValue, rule.RequiredAmount);
        if (clampedValue != nextValue)
            PixelCrushersQuestBridge.SetIntVariable(rule.ProgressVariableName, clampedValue);

#if UNITY_EDITOR
        if (_debugMapping)
            Debug.Log($"[PixelCrushersQuestProgressMapper] '{rule.QuestName}' {rule.ProgressVariableName}={clampedValue}/{rule.RequiredAmount}.", this);
#endif

        if (clampedValue < rule.RequiredAmount)
            return;

        PixelCrushersQuestBridge.SetQuestEntryState(rule.QuestName, rule.EntryNumber, rule.EntryCompleteState);
        PixelCrushersQuestBridge.SetQuestState(rule.QuestName, rule.QuestCompleteState);

#if UNITY_EDITOR
        if (_debugMapping)
            Debug.Log($"[PixelCrushersQuestProgressMapper] Quest '{rule.QuestName}' reached '{PixelCrushersQuestBridge.GetQuestStateString(rule.QuestName)}'.", this);
#endif
    }
}

[Serializable]
public class QuestFactProgressRule
{
    [Header("Fact Match")]
    public QuestFactType FactType = QuestFactType.Kill;
    public string ExactId = string.Empty;
    public string TypeOrTag = string.Empty;
    public string ContextId = string.Empty;

    [Header("Pixel Crushers Progress")]
    public string QuestName = string.Empty;
    public bool RequireQuestActive = true;
    public string ProgressVariableName = string.Empty;
    [Min(1)] public int RequiredAmount = 1;

    [Header("Completion")]
    [Min(1)] public int EntryNumber = 1;
    public QuestState EntryCompleteState = QuestState.Success;
    public QuestState QuestCompleteState = QuestState.ReturnToNPC;

    public bool Matches(QuestFact fact)
    {
        if (FactType != fact.Type)
            return false;

        if (!MatchesOptional(ExactId, fact.ExactId))
            return false;

        if (!MatchesOptional(TypeOrTag, fact.TypeOrTag))
            return false;

        if (!MatchesOptional(ContextId, fact.ContextId))
            return false;

        return !string.IsNullOrWhiteSpace(QuestName)
               && !string.IsNullOrWhiteSpace(ProgressVariableName);
    }

    private static bool MatchesOptional(string expected, string actual)
    {
        return string.IsNullOrWhiteSpace(expected)
               || string.Equals(expected, actual, StringComparison.Ordinal);
    }
}
