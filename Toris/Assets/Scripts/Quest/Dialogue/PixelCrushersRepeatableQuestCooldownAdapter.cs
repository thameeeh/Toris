using System;
using System.Collections.Generic;
using PixelCrushers.DialogueSystem;
using UnityEngine;

/// <summary>
/// Reopens completed repeatable Pixel Crushers quests after their rewards are claimed and cooldown expires.
/// Pixel Crushers still owns quest state; this adapter only handles repeatable job lifecycle glue.
/// </summary>
[AddComponentMenu("Toris/Quest/Pixel Crushers Repeatable Quest Cooldown Adapter")]
public class PixelCrushersRepeatableQuestCooldownAdapter : MonoBehaviour
{
    [Tooltip("Repeatable quest cooldown set assets watched by this adapter.")]
    [SerializeField] private PixelCrushersRepeatableQuestCooldownSetSO[] _cooldownSets = Array.Empty<PixelCrushersRepeatableQuestCooldownSetSO>();
    [Tooltip("Creates a DontDestroyOnLoad runtime adapter from these cooldown sets. Keep enabled when repeatable jobs must survive scene changes.")]
    [SerializeField] private bool _installPersistentRuntime = true;

#if UNITY_EDITOR
    [Header("Debug")]
    [Tooltip("Logs cooldown starts and repeatable quest resets. Editor only.")]
    [SerializeField] private bool _debugCooldowns = true;
#endif

    private static PixelCrushersRepeatableQuestCooldownAdapter _persistentRuntime;
    private static bool _creatingPersistentRuntime;

    private readonly List<PixelCrushersRepeatableQuestCooldownDefinition> _runtimeCooldowns = new List<PixelCrushersRepeatableQuestCooldownDefinition>();
    private bool _isPersistentRuntime;

    private void Awake()
    {
        if (_creatingPersistentRuntime)
            return;

        if (!isActiveAndEnabled)
            return;

        if (_installPersistentRuntime && !_isPersistentRuntime)
            InstallPersistentRuntime();
    }

    private void OnEnable()
    {
        if (_creatingPersistentRuntime)
            return;

        if (_installPersistentRuntime && !_isPersistentRuntime)
        {
            InstallPersistentRuntime();
            return;
        }

        RebuildRuntimeCooldowns();
    }

    private void OnDestroy()
    {
        if (_persistentRuntime == this)
            _persistentRuntime = null;
    }

    private void Update()
    {
        if (_installPersistentRuntime && !_isPersistentRuntime)
            return;

        if (!PixelCrushersQuestBridge.HasDialogueManager || _runtimeCooldowns.Count == 0)
            return;

        int currentUtc = GetCurrentUtcUnixTimeSeconds();
        for (int i = 0; i < _runtimeCooldowns.Count; i++)
            ProcessCooldown(_runtimeCooldowns[i], currentUtc);
    }

    private void InstallPersistentRuntime()
    {
        if (_persistentRuntime != null)
            return;

        List<PixelCrushersRepeatableQuestCooldownDefinition> cooldowns = BuildRuntimeCooldowns();
        if (cooldowns.Count == 0)
            return;

        GameObject runtimeObject = new GameObject("Pixel Crushers Repeatable Quest Cooldown Adapter");
        DontDestroyOnLoad(runtimeObject);

        PixelCrushersRepeatableQuestCooldownAdapter runtime;
        try
        {
            _creatingPersistentRuntime = true;
            runtime = runtimeObject.AddComponent<PixelCrushersRepeatableQuestCooldownAdapter>();
        }
        finally
        {
            _creatingPersistentRuntime = false;
        }

        _persistentRuntime = runtime;
#if UNITY_EDITOR
        runtime.ConfigurePersistentRuntime(cooldowns, _debugCooldowns);
        if (_debugCooldowns)
            Debug.Log($"[PixelCrushersRepeatableQuestCooldownAdapter] Installed persistent runtime with {cooldowns.Count} repeatable quest(s).", this);
#else
        runtime.ConfigurePersistentRuntime(cooldowns);
#endif
    }

#if UNITY_EDITOR
    private void ConfigurePersistentRuntime(List<PixelCrushersRepeatableQuestCooldownDefinition> cooldowns, bool debugCooldowns)
#else
    private void ConfigurePersistentRuntime(List<PixelCrushersRepeatableQuestCooldownDefinition> cooldowns)
#endif
    {
        _installPersistentRuntime = false;
        _isPersistentRuntime = true;
        _runtimeCooldowns.Clear();
        _runtimeCooldowns.AddRange(cooldowns ?? new List<PixelCrushersRepeatableQuestCooldownDefinition>());

#if UNITY_EDITOR
        _debugCooldowns = debugCooldowns;
#endif
    }

    private void RebuildRuntimeCooldowns()
    {
        _runtimeCooldowns.Clear();
        _runtimeCooldowns.AddRange(BuildRuntimeCooldowns());
    }

    private List<PixelCrushersRepeatableQuestCooldownDefinition> BuildRuntimeCooldowns()
    {
        List<PixelCrushersRepeatableQuestCooldownDefinition> cooldowns = new List<PixelCrushersRepeatableQuestCooldownDefinition>();

        if (_cooldownSets != null)
        {
            for (int i = 0; i < _cooldownSets.Length; i++)
                _cooldownSets[i]?.AppendCooldownsTo(cooldowns);
        }

        return cooldowns;
    }

    private void ProcessCooldown(PixelCrushersRepeatableQuestCooldownDefinition cooldown, int currentUtc)
    {
        if (cooldown == null || !cooldown.IsConfigured)
            return;

        int cooldownEndUtc = PixelCrushersQuestBridge.GetIntVariable(cooldown.ResolvedCooldownEndVariableName, 0);
        if (cooldownEndUtc > 0)
        {
            if (currentUtc < cooldownEndUtc)
                return;

            CompleteCooldown(cooldown);
            return;
        }

        QuestState questState = PixelCrushersQuestBridge.GetQuestState(cooldown.QuestName);
        if (questState == QuestState.Unassigned && HasCompletedRepeatableBefore(cooldown))
        {
            RepairCompletedCooldownState(cooldown);
            return;
        }

        if (questState != QuestState.Success)
            return;

        if (PixelCrushersQuestRewardAdapter.HasUnclaimedRewards(cooldown.QuestName))
            return;

        StartCooldown(cooldown, currentUtc);
    }

    private void StartCooldown(PixelCrushersRepeatableQuestCooldownDefinition cooldown, int currentUtc)
    {
        int cooldownEndUtc = AddCooldownSeconds(currentUtc, cooldown.CooldownSeconds);
        PixelCrushersQuestBridge.SetIntVariable(cooldown.ResolvedCooldownEndVariableName, cooldownEndUtc);
        int completionCount = PixelCrushersQuestBridge.IncrementIntVariable(cooldown.ResolvedCompletionCountVariableName);

#if UNITY_EDITOR
        if (_debugCooldowns)
            Debug.Log($"[PixelCrushersRepeatableQuestCooldownAdapter] Started cooldown for '{cooldown.QuestName}' until UTC {cooldownEndUtc}. Completion count={completionCount}.", this);
#endif

        if (cooldown.CooldownSeconds <= 0)
            CompleteCooldown(cooldown);
    }

    private void CompleteCooldown(PixelCrushersRepeatableQuestCooldownDefinition cooldown)
    {
        ResetRepeatableForNextRun(cooldown);
        PixelCrushersQuestBridge.SetIntVariable(cooldown.ResolvedCooldownEndVariableName, 0);
        PixelCrushersQuestBridge.SetQuestState(cooldown.QuestName, cooldown.ResolvedAvailableStateAfterCooldown);

#if UNITY_EDITOR
        if (_debugCooldowns)
        {
            if (cooldown.AvailableStateAfterCooldown != cooldown.ResolvedAvailableStateAfterCooldown)
                Debug.LogWarning($"[PixelCrushersRepeatableQuestCooldownAdapter] '{cooldown.QuestName}' had an unavailable Available State After Cooldown value '{cooldown.AvailableStateAfterCooldown}'. Treating it as Grantable so the repeatable job can appear again.", this);

            Debug.Log($"[PixelCrushersRepeatableQuestCooldownAdapter] Cooldown complete for '{cooldown.QuestName}'. Quest is now '{PixelCrushersQuestBridge.GetQuestStateString(cooldown.QuestName)}'.", this);
        }
#endif
    }

    private void RepairCompletedCooldownState(PixelCrushersRepeatableQuestCooldownDefinition cooldown)
    {
        ResetRepeatableForNextRun(cooldown);
        PixelCrushersQuestBridge.SetQuestState(cooldown.QuestName, cooldown.ResolvedAvailableStateAfterCooldown);

#if UNITY_EDITOR
        if (_debugCooldowns)
            Debug.LogWarning($"[PixelCrushersRepeatableQuestCooldownAdapter] Repaired repeatable quest '{cooldown.QuestName}' from Unassigned to '{PixelCrushersQuestBridge.GetQuestStateString(cooldown.QuestName)}' because it has previous completions and no active cooldown.", this);
#endif
    }

    private static bool HasCompletedRepeatableBefore(PixelCrushersRepeatableQuestCooldownDefinition cooldown)
    {
        return PixelCrushersQuestBridge.GetIntVariable(cooldown.ResolvedCompletionCountVariableName, 0) > 0;
    }

    private static void ResetRepeatableForNextRun(PixelCrushersRepeatableQuestCooldownDefinition cooldown)
    {
        ResetProgressVariables(cooldown);

        if (cooldown.ResetAllQuestEntries)
            ResetQuestEntries(cooldown);

        if (cooldown.ResetRewardClaimState)
        {
            ResetRewardVariables(cooldown);
            PixelCrushersQuestRewardAdapter.ResetRewardClaimState(cooldown.QuestName);
        }
    }

    private static void ResetProgressVariables(PixelCrushersRepeatableQuestCooldownDefinition cooldown)
    {
        if (cooldown.ProgressVariableNamesToReset == null)
            return;

        for (int i = 0; i < cooldown.ProgressVariableNamesToReset.Length; i++)
            PixelCrushersQuestBridge.SetIntVariable(cooldown.ProgressVariableNamesToReset[i], 0);
    }

    private static void ResetRewardVariables(PixelCrushersRepeatableQuestCooldownDefinition cooldown)
    {
        string rewardGrantedVariable = cooldown.ResolvedRewardGrantedVariableNameToReset;
        if (string.IsNullOrWhiteSpace(rewardGrantedVariable))
            return;

        PixelCrushersQuestBridge.SetIntVariable(rewardGrantedVariable, 0);
        PixelCrushersQuestBridge.SetIntVariable($"{rewardGrantedVariable}_Gold", 0);
        PixelCrushersQuestBridge.SetIntVariable($"{rewardGrantedVariable}_Experience", 0);
        PixelCrushersQuestBridge.SetIntVariable($"{rewardGrantedVariable}_Item", 0);
    }

    private static void ResetQuestEntries(PixelCrushersRepeatableQuestCooldownDefinition cooldown)
    {
        int entryCount = PixelCrushersQuestBridge.GetQuestEntryCount(cooldown.QuestName);
        for (int entryNumber = 1; entryNumber <= entryCount; entryNumber++)
            PixelCrushersQuestBridge.SetQuestEntryState(cooldown.QuestName, entryNumber, cooldown.QuestEntryResetState);
    }

    private static int GetCurrentUtcUnixTimeSeconds()
    {
        DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return Mathf.Max(0, (int)(DateTime.UtcNow - epoch).TotalSeconds);
    }

    private static int AddCooldownSeconds(int currentUtc, int cooldownSeconds)
    {
        int safeCooldownSeconds = Mathf.Max(0, cooldownSeconds);
        if (safeCooldownSeconds > int.MaxValue - currentUtc)
            return int.MaxValue;

        return currentUtc + safeCooldownSeconds;
    }
}
