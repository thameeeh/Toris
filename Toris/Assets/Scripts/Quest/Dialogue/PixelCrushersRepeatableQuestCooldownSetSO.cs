using System;
using System.Collections.Generic;
using PixelCrushers.DialogueSystem;
using UnityEngine;

/// <summary>
/// Reusable repeatable quest cooldown table keyed by Pixel Crushers quest names.
/// Use this for jobs that should become grantable again after rewards are claimed and a cooldown expires.
/// </summary>
[CreateAssetMenu(
    fileName = "PixelCrushersRepeatableQuestCooldownSet",
    menuName = "Quest/Pixel Crushers/Repeatable Quest Cooldown Set")]
public class PixelCrushersRepeatableQuestCooldownSetSO : ScriptableObject
{
    [Tooltip("Repeatable quest cooldown entries keyed by Pixel Crushers quest name.")]
    [SerializeField] private PixelCrushersRepeatableQuestCooldownDefinition[] _cooldowns = Array.Empty<PixelCrushersRepeatableQuestCooldownDefinition>();

    public int CooldownCount => _cooldowns == null ? 0 : _cooldowns.Length;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_cooldowns == null)
            return;

        for (int i = 0; i < _cooldowns.Length; i++)
            _cooldowns[i]?.ValidateDefaults();
    }
#endif

    public void AppendCooldownsTo(List<PixelCrushersRepeatableQuestCooldownDefinition> target)
    {
        if (target == null || _cooldowns == null)
            return;

        for (int i = 0; i < _cooldowns.Length; i++)
        {
            if (_cooldowns[i] != null && _cooldowns[i].IsConfigured)
                target.Add(_cooldowns[i]);
        }
    }
}

[Serializable]
public class PixelCrushersRepeatableQuestCooldownDefinition
{
    [Header("Pixel Crushers Quest")]
    [Tooltip("Pixel Crushers quest name that should become repeatable after completion.")]
    public string QuestName = string.Empty;

    [Header("Cooldown")]
    [Tooltip("Real-world seconds to wait after rewards are fully claimed before setting the quest back to Grantable.")]
    [Min(0)] public int CooldownSeconds = 60;
    [Tooltip("Pixel Crushers Lua variable that stores the UTC unix timestamp when the cooldown ends. Leave blank to use QuestName_CooldownEndsAtUtc.")]
    public string CooldownEndVariableName = string.Empty;
    [Tooltip("Pixel Crushers Lua variable that stores how many times this quest has entered cooldown. Leave blank to use QuestName_CompletionCount.")]
    public string CompletionCountVariableName = string.Empty;

    [Header("Reset When Cooldown Ends")]
    [Tooltip("Pixel Crushers progress variables to reset to 0 when the cooldown ends. Example: GuideCullWolfKills.")]
    public string[] ProgressVariableNamesToReset = Array.Empty<string>();
    [Tooltip("Reset all quest entry states when the cooldown ends so the next acceptance starts cleanly.")]
    public bool ResetAllQuestEntries = true;
    [Tooltip("Entry state assigned when resetting entries for the next repeat.")]
    public QuestState QuestEntryResetState = QuestState.Unassigned;
    [Tooltip("Quest state assigned when the cooldown ends.")]
    public QuestState AvailableStateAfterCooldown = QuestState.Grantable;

    [Header("Reward Reset")]
    [Tooltip("Reset reward claim guard variables when the cooldown ends so the next completion can pay again.")]
    public bool ResetRewardClaimState = true;
    [Tooltip("Full reward guard variable to reset. Leave blank to use QuestName_RewardsGranted. Match the reward definition if it uses a custom guard name.")]
    public string RewardGrantedVariableNameToReset = string.Empty;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(QuestName);

    public string ResolvedCooldownEndVariableName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(CooldownEndVariableName))
                return CooldownEndVariableName;

            return PixelCrushersQuestNaming.CooldownEndVariable(QuestName);
        }
    }

    public string ResolvedCompletionCountVariableName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(CompletionCountVariableName))
                return CompletionCountVariableName;

            return PixelCrushersQuestNaming.CompletionCountVariable(QuestName);
        }
    }

    public string ResolvedRewardGrantedVariableNameToReset
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(RewardGrantedVariableNameToReset))
                return RewardGrantedVariableNameToReset;

            return PixelCrushersQuestNaming.RewardGrantedVariable(QuestName);
        }
    }

    public QuestState ResolvedAvailableStateAfterCooldown
    {
        get
        {
            return NormalizeAvailableStateAfterCooldown(AvailableStateAfterCooldown);
        }
    }

#if UNITY_EDITOR
    public void ValidateDefaults()
    {
        CooldownSeconds = Mathf.Max(0, CooldownSeconds);
        QuestEntryResetState = NormalizeQuestEntryResetState(QuestEntryResetState);
        AvailableStateAfterCooldown = NormalizeAvailableStateAfterCooldown(AvailableStateAfterCooldown);
    }
#endif

    private static QuestState NormalizeAvailableStateAfterCooldown(QuestState state)
    {
        switch (state)
        {
            case QuestState.Active:
            case QuestState.Success:
            case QuestState.Failure:
            case QuestState.Abandoned:
            case QuestState.Grantable:
            case QuestState.ReturnToNPC:
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
