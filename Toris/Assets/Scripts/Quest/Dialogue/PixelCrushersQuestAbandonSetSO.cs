using System;
using System.Collections.Generic;
using PixelCrushers.DialogueSystem;
using UnityEngine;

/// <summary>
/// Reusable abandon rules keyed by Pixel Crushers quest names.
/// Pixel Crushers owns quest state; this asset only describes Toris-side abandon behavior.
/// </summary>
[CreateAssetMenu(
    fileName = "PixelCrushersQuestAbandonSet",
    menuName = "Quest/Pixel Crushers/Quest Abandon Set")]
public class PixelCrushersQuestAbandonSetSO : ScriptableObject
{
    [Tooltip("Abandon entries keyed by Pixel Crushers quest name. Only quests listed here can use the Toris abandon flow.")]
    [SerializeField] private PixelCrushersQuestAbandonDefinition[] _abandons = Array.Empty<PixelCrushersQuestAbandonDefinition>();

    public int AbandonCount => _abandons == null ? 0 : _abandons.Length;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_abandons == null)
            return;

        for (int i = 0; i < _abandons.Length; i++)
            _abandons[i]?.ValidateDefaults();
    }
#endif

    public void AppendAbandonsTo(List<PixelCrushersQuestAbandonDefinition> target)
    {
        if (target == null || _abandons == null)
            return;

        for (int i = 0; i < _abandons.Length; i++)
        {
            if (_abandons[i] != null && _abandons[i].IsConfigured)
                target.Add(_abandons[i]);
        }
    }
}

[Serializable]
public class PixelCrushersQuestAbandonDefinition
{
    private const float PercentDenominator = 100f;

    [Header("Pixel Crushers Quest")]
    [Tooltip("Pixel Crushers quest name that can be abandoned.")]
    public string QuestName = string.Empty;
    [Tooltip("When disabled, this quest is protected even if it appears in this abandon set.")]
    public bool CanAbandon = true;

    [Header("After Abandon")]
    [Tooltip("Quest state assigned after abandoning. Use Grantable for reusable jobs, Unassigned for hidden/locked work, or Abandoned for permanent abandoned state.")]
    public QuestState StateAfterAbandon = QuestState.Grantable;
    [Tooltip("Pixel Crushers progress variables to reset to 0 when abandoning. Example: GuideCullWolfKills.")]
    public string[] ProgressVariableNamesToReset = Array.Empty<string>();
    [Tooltip("Reset all quest entry states when abandoning so the next acceptance starts cleanly.")]
    public bool ResetAllQuestEntries = true;
    [Tooltip("Entry state assigned when resetting quest entries.")]
    public QuestState QuestEntryResetState = QuestState.Unassigned;
    [Tooltip("Reset reward claim guard variables when abandoning so unfinished reward state cannot leak into the next run.")]
    public bool ResetRewardClaimState = true;
    [Tooltip("Reset the cooldown end timestamp variable when abandoning. Leave enabled for repeatable jobs.")]
    public bool ResetCooldownState = true;
    [Tooltip("Pixel Crushers Lua variable that stores the cooldown end timestamp. Leave blank to use QuestName_CooldownEndsAtUtc.")]
    public string CooldownEndVariableNameToReset = string.Empty;
    [Tooltip("Pixel Crushers Lua variable that stores how many times this quest was abandoned. Leave blank to use QuestName_AbandonCount.")]
    public string AbandonCountVariableName = string.Empty;

    [Header("Penalty")]
    [Tooltip("Flat gold removed when abandoning. Clamped to the player's current gold.")]
    [Min(0)] public int FlatGoldPenalty;
    [Tooltip("Percent of current gold removed when abandoning. Example: 5 = lose 5% of current gold.")]
    [Range(0f, 100f)] public float GoldPenaltyPercent;
    [Tooltip("Flat XP removed when abandoning. Clamped to the player's current XP.")]
    [Min(0)] public int FlatExperiencePenalty;
    [Tooltip("Percent of current XP removed when abandoning. Example: 5 = lose 5% of current XP.")]
    [Range(0f, 100f)] public float ExperiencePenaltyPercent;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(QuestName);

    public string ResolvedCooldownEndVariableNameToReset
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(CooldownEndVariableNameToReset))
                return CooldownEndVariableNameToReset;

            return PixelCrushersQuestNaming.CooldownEndVariable(QuestName);
        }
    }

    public string ResolvedAbandonCountVariableName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(AbandonCountVariableName))
                return AbandonCountVariableName;

            return PixelCrushersQuestNaming.AbandonCountVariable(QuestName);
        }
    }

    public QuestState ResolvedStateAfterAbandon
    {
        get
        {
            return NormalizeStateAfterAbandon(StateAfterAbandon);
        }
    }

    public int CalculateGoldPenalty(int currentGold)
    {
        int percentPenalty = Mathf.RoundToInt(Mathf.Max(0, currentGold) * Mathf.Clamp(GoldPenaltyPercent, 0f, 100f) / PercentDenominator);
        return Mathf.Clamp(FlatGoldPenalty + percentPenalty, 0, Mathf.Max(0, currentGold));
    }

    public int CalculateExperiencePenalty(float currentExperience)
    {
        int currentExperienceInt = Mathf.FloorToInt(Mathf.Max(0f, currentExperience));
        int percentPenalty = Mathf.RoundToInt(currentExperienceInt * Mathf.Clamp(ExperiencePenaltyPercent, 0f, 100f) / PercentDenominator);
        return Mathf.Clamp(FlatExperiencePenalty + percentPenalty, 0, currentExperienceInt);
    }

#if UNITY_EDITOR
    public void ValidateDefaults()
    {
        StateAfterAbandon = NormalizeStateAfterAbandon(StateAfterAbandon);
        QuestEntryResetState = NormalizeQuestEntryResetState(QuestEntryResetState);
        FlatGoldPenalty = Mathf.Max(0, FlatGoldPenalty);
        GoldPenaltyPercent = Mathf.Clamp(GoldPenaltyPercent, 0f, 100f);
        FlatExperiencePenalty = Mathf.Max(0, FlatExperiencePenalty);
        ExperiencePenaltyPercent = Mathf.Clamp(ExperiencePenaltyPercent, 0f, 100f);
    }
#endif

    private static QuestState NormalizeStateAfterAbandon(QuestState state)
    {
        switch (state)
        {
            case QuestState.Unassigned:
            case QuestState.Abandoned:
            case QuestState.Grantable:
                return state;
            default:
                return QuestState.Grantable;
        }
    }

    private static QuestState NormalizeQuestEntryResetState(QuestState state)
    {
        switch (state)
        {
            case QuestState.Unassigned:
            case QuestState.Active:
            case QuestState.Success:
            case QuestState.Failure:
            case QuestState.Abandoned:
            case QuestState.Grantable:
            case QuestState.ReturnToNPC:
                return state;
            default:
                return QuestState.Unassigned;
        }
    }
}
