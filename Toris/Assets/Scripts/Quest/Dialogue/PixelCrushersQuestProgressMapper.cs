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
    [Tooltip("Optional fallback mapping that progresses active quests from Pixel Crushers variables named QuestName_FactType_Target or QuestName_FactType_Target_Required_3.")]
    [SerializeField] private QuestFactConventionProgressSettings _conventionProgress = new QuestFactConventionProgressSettings();
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
        if (rules.Length == 0 && !IsConventionProgressEnabled())
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
        runtime.ConfigurePersistentRuntime(rules, _conventionProgress, _debugMapping);
        if (_debugMapping)
            Debug.Log($"[PixelCrushersQuestProgressMapper] Installed persistent runtime with {rules.Length} explicit rule(s).", this);
#else
        runtime.ConfigurePersistentRuntime(rules, _conventionProgress);
#endif
    }

#if UNITY_EDITOR
    private void ConfigurePersistentRuntime(QuestFactProgressRule[] rules, QuestFactConventionProgressSettings conventionProgress, bool debugMapping)
#else
    private void ConfigurePersistentRuntime(QuestFactProgressRule[] rules, QuestFactConventionProgressSettings conventionProgress)
#endif
    {
        _runtimeRules = rules ?? Array.Empty<QuestFactProgressRule>();
        _conventionProgress = conventionProgress == null ? new QuestFactConventionProgressSettings() : conventionProgress.Clone();
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
        if (!PixelCrushersQuestBridge.HasDialogueManager)
            return;

        HashSet<string> appliedProgressKeys = null;
        QuestFactProgressRule[] rules = GetRulesForFactProcessing();
        for (int i = 0; i < rules.Length; i++)
        {
            QuestFactProgressRule rule = rules[i];
            if (rule == null || !rule.Matches(fact))
                continue;

            if (!ApplyRule(rule, fact))
                continue;

            if (appliedProgressKeys == null)
                appliedProgressKeys = new HashSet<string>(StringComparer.Ordinal);

            appliedProgressKeys.Add(BuildProgressKey(rule.QuestName, rule.ResolvedProgressVariableName));
        }

        ApplyConventionProgress(fact, appliedProgressKeys);
    }

    private bool ApplyRule(QuestFactProgressRule rule, QuestFact fact)
    {
        if (rule.RequireQuestActive && PixelCrushersQuestBridge.GetQuestState(rule.QuestName) != QuestState.Active)
            return false;

        string progressVariableName = rule.ResolvedProgressVariableName;
        if (string.IsNullOrWhiteSpace(progressVariableName))
            return false;

        int nextValue = PixelCrushersQuestBridge.IncrementIntVariable(progressVariableName, fact.Amount);
        int clampedValue = Mathf.Min(nextValue, rule.RequiredAmount);
        if (clampedValue != nextValue)
            PixelCrushersQuestBridge.SetIntVariable(progressVariableName, clampedValue);

#if UNITY_EDITOR
        if (_debugMapping)
            Debug.Log($"[PixelCrushersQuestProgressMapper] '{rule.QuestName}' {progressVariableName}={clampedValue}/{rule.RequiredAmount}.", this);
#endif

        if (clampedValue < rule.RequiredAmount)
            return true;

        PixelCrushersQuestBridge.SetQuestEntryState(rule.QuestName, rule.EntryNumber, rule.EntryCompleteState);
        PixelCrushersQuestBridge.SetQuestState(rule.QuestName, rule.QuestCompleteState);

#if UNITY_EDITOR
        if (_debugMapping)
            Debug.Log($"[PixelCrushersQuestProgressMapper] Quest '{rule.QuestName}' reached '{PixelCrushersQuestBridge.GetQuestStateString(rule.QuestName)}'.", this);
#endif

        return true;
    }

    private bool IsConventionProgressEnabled()
    {
        if (_conventionProgress == null)
            _conventionProgress = new QuestFactConventionProgressSettings();

        return _conventionProgress.Enabled;
    }

    private void ApplyConventionProgress(QuestFact fact, HashSet<string> appliedProgressKeys)
    {
        if (!IsConventionProgressEnabled() || fact.Type == QuestFactType.None)
            return;

        DialogueDatabase database = DialogueManager.masterDatabase;
        if (database == null || database.items == null || database.variables == null)
            return;

        string factSegment = PixelCrushersQuestNaming.SanitizeSegment(fact.Type.ToString());
        if (string.IsNullOrWhiteSpace(factSegment))
            return;

        for (int i = 0; i < database.items.Count; i++)
        {
            Item quest = database.items[i];
            if (quest == null || quest.IsItem)
                continue;

            string questName = quest.Name;
            if (string.IsNullOrWhiteSpace(questName))
                continue;

            if (_conventionProgress.RequireQuestActive && PixelCrushersQuestBridge.GetQuestState(questName) != QuestState.Active)
                continue;

            ApplyConventionProgressForQuest(fact, questName, factSegment, database.variables, appliedProgressKeys);
        }
    }

    private void ApplyConventionProgressForQuest(
        QuestFact fact,
        string questName,
        string factSegment,
        List<Variable> variables,
        HashSet<string> appliedProgressKeys)
    {
        string questSegment = PixelCrushersQuestNaming.SanitizeSegment(questName);
        if (string.IsNullOrWhiteSpace(questSegment))
            return;

        string variablePrefix = $"{questSegment}_{factSegment}_";
        for (int i = 0; i < variables.Count; i++)
        {
            Variable variable = variables[i];
            if (variable == null || string.IsNullOrWhiteSpace(variable.Name))
                continue;

            string variableName = variable.Name.Trim();
            if (!variableName.StartsWith(variablePrefix, StringComparison.Ordinal))
                continue;

            int requiredAmount;
            if (!TryParseConventionProgressVariable(variableName, variablePrefix.Length, fact, out requiredAmount))
                continue;

            string progressKey = BuildProgressKey(questName, variableName);
            if (appliedProgressKeys != null && appliedProgressKeys.Contains(progressKey))
                continue;

            ApplyConventionProgressVariable(fact, questName, variableName, requiredAmount);
        }
    }

    private void ApplyConventionProgressVariable(QuestFact fact, string questName, string progressVariableName, int requiredAmount)
    {
        int safeRequiredAmount = Mathf.Max(1, requiredAmount);
        int nextValue = PixelCrushersQuestBridge.IncrementIntVariable(progressVariableName, fact.Amount);
        int clampedValue = Mathf.Min(nextValue, safeRequiredAmount);
        if (clampedValue != nextValue)
            PixelCrushersQuestBridge.SetIntVariable(progressVariableName, clampedValue);

#if UNITY_EDITOR
        if (_debugMapping)
            Debug.Log($"[PixelCrushersQuestProgressMapper] Convention '{questName}' {progressVariableName}={clampedValue}/{safeRequiredAmount}.", this);
#endif

        if (clampedValue < safeRequiredAmount)
            return;

        int entryNumber = Mathf.Max(1, _conventionProgress.EntryNumber);
        PixelCrushersQuestBridge.SetQuestEntryState(questName, entryNumber, _conventionProgress.EntryCompleteState);
        PixelCrushersQuestBridge.SetQuestState(questName, _conventionProgress.QuestCompleteState);

#if UNITY_EDITOR
        if (_debugMapping)
            Debug.Log($"[PixelCrushersQuestProgressMapper] Convention quest '{questName}' reached '{PixelCrushersQuestBridge.GetQuestStateString(questName)}'.", this);
#endif
    }

    private bool TryParseConventionProgressVariable(string variableName, int targetStartIndex, QuestFact fact, out int requiredAmount)
    {
        requiredAmount = Mathf.Max(1, _conventionProgress.DefaultRequiredAmount);

        if (targetStartIndex < 0 || targetStartIndex >= variableName.Length)
            return false;

        string targetSegment = variableName.Substring(targetStartIndex);
        string requiredMarker = $"_{PixelCrushersQuestNaming.RequiredAmountSegment}_";
        int requiredMarkerIndex = targetSegment.LastIndexOf(requiredMarker, StringComparison.Ordinal);
        if (requiredMarkerIndex >= 0)
        {
            string requiredAmountText = targetSegment.Substring(requiredMarkerIndex + requiredMarker.Length);
            int parsedRequiredAmount;
            if (!int.TryParse(requiredAmountText, out parsedRequiredAmount) || parsedRequiredAmount < 1)
                return false;

            requiredAmount = parsedRequiredAmount;
            targetSegment = targetSegment.Substring(0, requiredMarkerIndex);
        }

        return MatchesConventionTarget(targetSegment, fact);
    }

    private static bool MatchesConventionTarget(string targetSegment, QuestFact fact)
    {
        string safeTarget = PixelCrushersQuestNaming.SanitizeSegment(targetSegment);
        if (string.IsNullOrWhiteSpace(safeTarget))
            return false;

        if (string.Equals(safeTarget, PixelCrushersQuestNaming.AnyTargetSegment, StringComparison.Ordinal))
            return true;

        return MatchesConventionTargetSegment(safeTarget, fact.ExactId)
               || MatchesConventionTargetSegment(safeTarget, fact.TypeOrTag)
               || MatchesConventionTargetSegment(safeTarget, fact.ContextId);
    }

    private static bool MatchesConventionTargetSegment(string expectedSegment, string actualValue)
    {
        string actualSegment = PixelCrushersQuestNaming.SanitizeSegment(actualValue);
        return !string.IsNullOrWhiteSpace(actualSegment)
               && string.Equals(expectedSegment, actualSegment, StringComparison.Ordinal);
    }

    private static string BuildProgressKey(string questName, string progressVariableName)
    {
        return $"{questName}|{progressVariableName}";
    }
}

[Serializable]
public class QuestFactConventionProgressSettings
{
    [Tooltip("When enabled, active quests can progress from Pixel Crushers variables named QuestName_FactType_Target or QuestName_FactType_Target_Required_3.")]
    public bool Enabled = true;
    [Tooltip("If enabled, convention variables only progress while their quest is active.")]
    public bool RequireQuestActive = true;
    [Tooltip("Required amount used when a convention variable does not end with _Required_#.")]
    [Min(1)] public int DefaultRequiredAmount = 1;
    [Tooltip("Pixel Crushers quest entry number to update when a convention variable reaches its required amount.")]
    [Min(1)] public int EntryNumber = 1;
    [Tooltip("State assigned to the quest entry when the convention variable reaches its required amount.")]
    public QuestState EntryCompleteState = QuestState.Success;
    [Tooltip("State assigned to the quest when the convention variable reaches its required amount.")]
    public QuestState QuestCompleteState = QuestState.ReturnToNPC;

    public QuestFactConventionProgressSettings Clone()
    {
        return new QuestFactConventionProgressSettings
        {
            Enabled = Enabled,
            RequireQuestActive = RequireQuestActive,
            DefaultRequiredAmount = DefaultRequiredAmount,
            EntryNumber = EntryNumber,
            EntryCompleteState = EntryCompleteState,
            QuestCompleteState = QuestCompleteState
        };
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
