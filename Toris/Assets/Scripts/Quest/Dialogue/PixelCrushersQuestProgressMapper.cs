using System;
using System.Collections.Generic;
using PixelCrushers.DialogueSystem;
using UnityEngine;

/// <summary>
/// Maps shared Toris gameplay facts into Pixel Crushers quest variables and states.
/// Keep quest progress rules here instead of spreading quest logic across gameplay scripts.
/// Add this once in a bootstrap scene and assign one or more QuestFactProgressRuleSetSO assets.
/// </summary>
public class PixelCrushersQuestProgressMapper : MonoBehaviour
{
    [Tooltip("Rule set assets that translate generic Toris facts into Pixel Crushers quest variables and states.")]
    [SerializeField] private QuestFactProgressRuleSetSO[] _ruleSets = Array.Empty<QuestFactProgressRuleSetSO>();
    [Tooltip("Creates a DontDestroyOnLoad runtime listener from these rule sets. Keep enabled when quests must progress across scene changes.")]
    [SerializeField] private bool _installPersistentRuntime = true;

#if UNITY_EDITOR
    [Header("Debug")]
    [Tooltip("Logs matched rules, variable progress, and quest state changes. Editor only.")]
    [SerializeField] private bool _debugMapping = true;
#endif

    private static PixelCrushersQuestProgressMapper _persistentRuntime;
    private static bool _creatingPersistentRuntime;

    private bool _isPersistentRuntime;
    private bool _isSubscribed;
    private QuestFactProgressRule[] _runtimeRules = Array.Empty<QuestFactProgressRule>();

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

        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void OnDestroy()
    {
        if (_persistentRuntime == this)
            _persistentRuntime = null;
    }

    private void InstallPersistentRuntime()
    {
        if (_persistentRuntime != null)
            return;

        QuestFactProgressRule[] rules = BuildRuntimeRules();
        if (rules.Length == 0)
            return;

        GameObject runtimeObject = new GameObject("Pixel Crushers Quest Progress Mapper");
        DontDestroyOnLoad(runtimeObject);

        PixelCrushersQuestProgressMapper runtime;
        try
        {
            _creatingPersistentRuntime = true;
            runtime = runtimeObject.AddComponent<PixelCrushersQuestProgressMapper>();
        }
        finally
        {
            _creatingPersistentRuntime = false;
        }

        _persistentRuntime = runtime;
#if UNITY_EDITOR
        runtime.ConfigurePersistentRuntime(rules, _debugMapping);
        if (_debugMapping)
            Debug.Log($"[PixelCrushersQuestProgressMapper] Installed persistent runtime with {rules.Length} rule(s).", this);
#else
        runtime.ConfigurePersistentRuntime(rules);
#endif
    }

#if UNITY_EDITOR
    private void ConfigurePersistentRuntime(QuestFactProgressRule[] rules, bool debugMapping)
#else
    private void ConfigurePersistentRuntime(QuestFactProgressRule[] rules)
#endif
    {
        _runtimeRules = rules ?? Array.Empty<QuestFactProgressRule>();
        _installPersistentRuntime = false;
        _isPersistentRuntime = true;

#if UNITY_EDITOR
        _debugMapping = debugMapping;
#endif

        Subscribe();
    }

    private QuestFactProgressRule[] BuildRuntimeRules()
    {
        List<QuestFactProgressRule> rules = new List<QuestFactProgressRule>();

        if (_ruleSets != null)
        {
            for (int i = 0; i < _ruleSets.Length; i++)
                _ruleSets[i]?.AppendRulesTo(rules);
        }

        return rules.ToArray();
    }

    private QuestFactProgressRule[] GetRulesForFactProcessing()
    {
        if (_isPersistentRuntime)
            return _runtimeRules;

        _runtimeRules = BuildRuntimeRules();
        return _runtimeRules;
    }

    private void Subscribe()
    {
        if (_isSubscribed)
            return;

        PixelCrushersQuestFactReporter.FactReported += HandleFactReported;
        _isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_isSubscribed)
            return;

        PixelCrushersQuestFactReporter.FactReported -= HandleFactReported;
        _isSubscribed = false;
    }

    private void HandleFactReported(QuestFact fact)
    {
        QuestFactProgressRule[] rules = GetRulesForFactProcessing();
        if (!PixelCrushersQuestBridge.HasDialogueManager || rules.Length == 0)
            return;

        for (int i = 0; i < rules.Length; i++)
        {
            QuestFactProgressRule rule = rules[i];
            if (rule == null || !rule.Matches(fact))
                continue;

            ApplyRule(rule, fact);
        }
    }

    private void ApplyRule(QuestFactProgressRule rule, QuestFact fact)
    {
        if (rule.RequireQuestActive && PixelCrushersQuestBridge.GetQuestState(rule.QuestName) != QuestState.Active)
            return;

        string progressVariableName = rule.ResolvedProgressVariableName;
        if (string.IsNullOrWhiteSpace(progressVariableName))
            return;

        int nextValue = PixelCrushersQuestBridge.IncrementIntVariable(progressVariableName, fact.Amount);
        int clampedValue = Mathf.Min(nextValue, rule.RequiredAmount);
        if (clampedValue != nextValue)
            PixelCrushersQuestBridge.SetIntVariable(progressVariableName, clampedValue);

#if UNITY_EDITOR
        if (_debugMapping)
            Debug.Log($"[PixelCrushersQuestProgressMapper] '{rule.QuestName}' {progressVariableName}={clampedValue}/{rule.RequiredAmount}.", this);
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
    [Tooltip("Type of gameplay fact this rule listens for, such as Kill, PickUp, InteractNpc, or VisitSite.")]
    public QuestFactType FactType = QuestFactType.Kill;
    [Tooltip("Optional exact target ID to match. Example: LeaderWolf or SmithNPC. Leave blank to match any exact ID.")]
    public string ExactId = string.Empty;
    [Tooltip("Optional broader target type/tag to match. Example: Wolf, Shopkeeper, Potion. Leave blank to ignore.")]
    public string TypeOrTag = string.Empty;
    [Tooltip("Optional context to match. Example: MainArea, Plains, Graveyard01. Leave blank to ignore.")]
    public string ContextId = string.Empty;

    [Header("Pixel Crushers Progress")]
    [Tooltip("Pixel Crushers quest name to progress when this rule matches.")]
    public string QuestName = string.Empty;
    [Tooltip("If enabled, this rule only progresses while the Pixel Crushers quest is active.")]
    public bool RequireQuestActive = true;
    [Tooltip("Pixel Crushers Lua variable used as the progress counter. Leave blank to use QuestName_FactType_TargetId.")]
    public string ProgressVariableName = string.Empty;
    [Tooltip("Amount needed before the quest entry and quest state are updated.")]
    [Min(1)] public int RequiredAmount = 1;

    [Header("Completion")]
    [Tooltip("Pixel Crushers quest entry number to update when the required amount is reached.")]
    [Min(1)] public int EntryNumber = 1;
    [Tooltip("State assigned to the quest entry when the required amount is reached.")]
    public QuestState EntryCompleteState = QuestState.Success;
    [Tooltip("State assigned to the overall quest when the required amount is reached. Usually ReturnToNPC.")]
    public QuestState QuestCompleteState = QuestState.ReturnToNPC;

    public string ResolvedProgressVariableName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(ProgressVariableName))
                return ProgressVariableName;

            return PixelCrushersQuestNaming.ProgressVariable(QuestName, FactType, ExactId, TypeOrTag, ContextId);
        }
    }

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
               && !string.IsNullOrWhiteSpace(ResolvedProgressVariableName);
    }

    private static bool MatchesOptional(string expected, string actual)
    {
        return string.IsNullOrWhiteSpace(expected)
               || string.Equals(expected, actual, StringComparison.Ordinal);
    }
}
