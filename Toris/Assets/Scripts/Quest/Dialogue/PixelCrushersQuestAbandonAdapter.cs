using System;
using System.Collections.Generic;
using System.Text;
using PixelCrushers.DialogueSystem;
using UnityEngine;

/// <summary>
/// Applies Toris-side abandon rules for Pixel Crushers quests.
/// Pixel Crushers still owns quest state; this adapter resets Toris variables and applies configured penalties.
/// </summary>
[AddComponentMenu("Toris/Quest/Pixel Crushers Quest Abandon Adapter")]
public class PixelCrushersQuestAbandonAdapter : MonoBehaviour
{
    [Header("Abandon Sets")]
    [Tooltip("Abandon set assets watched by this adapter. Only quests listed in these assets can use the Toris abandon flow.")]
    [SerializeField] private PixelCrushersQuestAbandonSetSO[] _abandonSets = Array.Empty<PixelCrushersQuestAbandonSetSO>();

    [Header("Penalty Targets")]
    [Tooltip("Runtime player progression anchor used to apply gold and XP penalties.")]
    [SerializeField] private PlayerProgressionAnchorSO _playerProgressionAnchor;

#if UNITY_EDITOR
    [Header("Debug")]
    [Tooltip("Logs abandon attempts and penalties. Editor only.")]
    [SerializeField] private bool _debugAbandonFlow = true;
#endif

    private static readonly List<PixelCrushersQuestAbandonAdapter> ActiveAdapters = new List<PixelCrushersQuestAbandonAdapter>();

    private readonly List<PixelCrushersQuestAbandonDefinition> _runtimeAbandons = new List<PixelCrushersQuestAbandonDefinition>();

    private void OnEnable()
    {
        if (!ActiveAdapters.Contains(this))
            ActiveAdapters.Add(this);

        RebuildRuntimeAbandons();
    }

    private void OnDisable()
    {
        ActiveAdapters.Remove(this);
    }

    public static bool CanAbandonQuest(string questName)
    {
        return TryGetAbandonDefinition(questName, out _)
            && PixelCrushersQuestBridge.GetQuestState(questName) == QuestState.Active;
    }

    public static bool TryGetAbandonPreviewText(string questName, out string previewText)
    {
        previewText = string.Empty;

        if (!TryGetAbandonDefinition(questName, out PixelCrushersQuestAbandonDefinition abandon))
            return false;

        StringBuilder builder = new StringBuilder();
        builder.Append("Returns quest to: ");
        builder.Append(QuestLog.StateToString(abandon.ResolvedStateAfterAbandon));

        AppendPenaltyPreview(builder, abandon);

        previewText = builder.ToString();
        return previewText.Length > 0;
    }

    public static bool TryAbandonQuest(string questName, out string resultText)
    {
        resultText = string.Empty;

        if (string.IsNullOrWhiteSpace(questName))
            return false;

        for (int i = 0; i < ActiveAdapters.Count; i++)
        {
            PixelCrushersQuestAbandonAdapter adapter = ActiveAdapters[i];
            if (adapter != null && adapter.TryAbandonQuestInternal(questName, out resultText))
                return true;
        }

        return false;
    }

    private static bool TryGetAbandonDefinition(string questName, out PixelCrushersQuestAbandonDefinition abandon)
    {
        abandon = null;

        if (string.IsNullOrWhiteSpace(questName))
            return false;

        for (int i = 0; i < ActiveAdapters.Count; i++)
        {
            PixelCrushersQuestAbandonAdapter adapter = ActiveAdapters[i];
            if (adapter != null && adapter.TryGetAbandonDefinitionInternal(questName, out abandon))
                return true;
        }

        return false;
    }

    private void RebuildRuntimeAbandons()
    {
        _runtimeAbandons.Clear();

        if (_abandonSets == null)
            return;

        for (int i = 0; i < _abandonSets.Length; i++)
            _abandonSets[i]?.AppendAbandonsTo(_runtimeAbandons);
    }

    private bool TryGetAbandonDefinitionInternal(string questName, out PixelCrushersQuestAbandonDefinition abandon)
    {
        abandon = null;

        for (int i = 0; i < _runtimeAbandons.Count; i++)
        {
            PixelCrushersQuestAbandonDefinition candidate = _runtimeAbandons[i];
            if (candidate == null || !candidate.CanAbandon)
                continue;

            if (!string.Equals(candidate.QuestName, questName, StringComparison.Ordinal))
                continue;

            abandon = candidate;
            return true;
        }

        return false;
    }

    private bool TryAbandonQuestInternal(string questName, out string resultText)
    {
        resultText = string.Empty;

        if (!TryGetAbandonDefinitionInternal(questName, out PixelCrushersQuestAbandonDefinition abandon))
            return false;

        if (PixelCrushersQuestBridge.GetQuestState(questName) != QuestState.Active)
            return false;

        int goldPenalty = ApplyGoldPenalty(abandon);
        int experiencePenalty = ApplyExperiencePenalty(abandon);

        ResetProgressVariables(abandon);

        if (abandon.ResetAllQuestEntries)
            ResetQuestEntries(abandon);

        if (abandon.ResetRewardClaimState)
            PixelCrushersQuestRewardAdapter.ResetRewardClaimState(questName);

        if (abandon.ResetCooldownState)
            PixelCrushersQuestBridge.SetIntVariable(abandon.ResolvedCooldownEndVariableNameToReset, 0);

        PixelCrushersQuestBridge.IncrementIntVariable(abandon.ResolvedAbandonCountVariableName);
        PixelCrushersQuestBridge.SetQuestState(questName, abandon.ResolvedStateAfterAbandon);

        resultText = BuildAbandonResultText(questName, goldPenalty, experiencePenalty);

#if UNITY_EDITOR
        if (_debugAbandonFlow)
            Debug.Log($"[PixelCrushersQuestAbandonAdapter] {resultText}", this);
#endif

        return true;
    }

    private int ApplyGoldPenalty(PixelCrushersQuestAbandonDefinition abandon)
    {
        if (_playerProgressionAnchor == null || !_playerProgressionAnchor.IsReady)
            return 0;

        PlayerProgression progression = _playerProgressionAnchor.Instance;
        int penalty = abandon.CalculateGoldPenalty(progression.CurrentGold);
        if (penalty <= 0)
            return 0;

        progression.SetGold(progression.CurrentGold - penalty);
        return penalty;
    }

    private int ApplyExperiencePenalty(PixelCrushersQuestAbandonDefinition abandon)
    {
        if (_playerProgressionAnchor == null || !_playerProgressionAnchor.IsReady)
            return 0;

        PlayerProgression progression = _playerProgressionAnchor.Instance;
        int penalty = abandon.CalculateExperiencePenalty(progression.CurrentExperience);
        if (penalty <= 0)
            return 0;

        progression.RemoveExperience(penalty);
        return penalty;
    }

    private static void ResetProgressVariables(PixelCrushersQuestAbandonDefinition abandon)
    {
        if (abandon.ProgressVariableNamesToReset == null)
            return;

        for (int i = 0; i < abandon.ProgressVariableNamesToReset.Length; i++)
            PixelCrushersQuestBridge.SetIntVariable(abandon.ProgressVariableNamesToReset[i], 0);
    }

    private static void ResetQuestEntries(PixelCrushersQuestAbandonDefinition abandon)
    {
        int entryCount = PixelCrushersQuestBridge.GetQuestEntryCount(abandon.QuestName);
        for (int entryNumber = 1; entryNumber <= entryCount; entryNumber++)
            PixelCrushersQuestBridge.SetQuestEntryState(abandon.QuestName, entryNumber, abandon.QuestEntryResetState);
    }

    private static void AppendPenaltyPreview(StringBuilder builder, PixelCrushersQuestAbandonDefinition abandon)
    {
        if (builder == null || abandon == null)
            return;

        if (abandon.FlatGoldPenalty <= 0
            && abandon.GoldPenaltyPercent <= 0f
            && abandon.FlatExperiencePenalty <= 0
            && abandon.ExperiencePenaltyPercent <= 0f)
        {
            builder.Append("\nPenalty: none");
            return;
        }

        builder.Append("\nPenalty:");

        if (abandon.FlatGoldPenalty > 0 || abandon.GoldPenaltyPercent > 0f)
            builder.Append($"\n- Gold: {FormatPenalty(abandon.FlatGoldPenalty, abandon.GoldPenaltyPercent)}");

        if (abandon.FlatExperiencePenalty > 0 || abandon.ExperiencePenaltyPercent > 0f)
            builder.Append($"\n- XP: {FormatPenalty(abandon.FlatExperiencePenalty, abandon.ExperiencePenaltyPercent)}");
    }

    private static string FormatPenalty(int flatPenalty, float percentPenalty)
    {
        if (flatPenalty > 0 && percentPenalty > 0f)
            return $"{flatPenalty} + {percentPenalty:0.#}% current";

        if (flatPenalty > 0)
            return flatPenalty.ToString();

        return $"{percentPenalty:0.#}% current";
    }

    private static string BuildAbandonResultText(string questName, int goldPenalty, int experiencePenalty)
    {
        string penaltyText = goldPenalty > 0 || experiencePenalty > 0
            ? $" Penalty: gold={goldPenalty}, xp={experiencePenalty}."
            : " No penalty applied.";

        return $"Abandoned quest '{questName}'.{penaltyText}";
    }
}
