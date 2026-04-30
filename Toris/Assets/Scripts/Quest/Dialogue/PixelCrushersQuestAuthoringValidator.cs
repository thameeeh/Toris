#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using PixelCrushers.DialogueSystem;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor-only validation for the Toris/Pixel Crushers quest bridge.
/// This catches string mismatch issues before they turn into invisible quest bugs in play mode.
/// </summary>
public sealed class PixelCrushersQuestAuthoringValidatorWindow : EditorWindow
{
    private Vector2 _scrollPosition;
    private PixelCrushersQuestAuthoringReport _report;

    [MenuItem("Tools/Toris/Quests/Quest Authoring Validator")]
    public static void Open()
    {
        GetWindow<PixelCrushersQuestAuthoringValidatorWindow>("Quest Validator");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Quest Authoring Validator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Scans Pixel Crushers quest database entries, Toris progress/reward/abandon/cooldown assets, job board journal modes, and dialogue command strings.",
            MessageType.Info);

        if (GUILayout.Button("Run Validation", GUILayout.Height(32f)))
        {
            _report = PixelCrushersQuestAuthoringValidator.Validate();
            PixelCrushersQuestAuthoringValidator.LogReport(_report);
        }

        if (_report == null)
            return;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField(_report.Summary, EditorStyles.boldLabel);

        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
        for (int i = 0; i < _report.Issues.Count; i++)
            DrawIssue(_report.Issues[i]);
        EditorGUILayout.EndScrollView();
    }

    private static void DrawIssue(PixelCrushersQuestAuthoringIssue issue)
    {
        MessageType messageType = ToMessageType(issue.Severity);
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.HelpBox(issue.Message, messageType);

        if (!string.IsNullOrWhiteSpace(issue.Source))
            EditorGUILayout.LabelField("Source", issue.Source);

        if (issue.Context != null)
            EditorGUILayout.ObjectField("Asset", issue.Context, typeof(UnityEngine.Object), false);

        EditorGUILayout.EndVertical();
    }

    private static MessageType ToMessageType(PixelCrushersQuestAuthoringSeverity severity)
    {
        switch (severity)
        {
            case PixelCrushersQuestAuthoringSeverity.Error:
                return MessageType.Error;
            case PixelCrushersQuestAuthoringSeverity.Warning:
                return MessageType.Warning;
            default:
                return MessageType.Info;
        }
    }
}

public static class PixelCrushersQuestAuthoringValidator
{
    private static readonly string[] QuestSearchRoots =
    {
        "Assets/Scripts/Quest"
    };

    private static readonly Regex QuestFunctionRegex = new Regex(
        @"\b(?:SetQuestState|SetQuestEntryState|CurrentQuestState)\s*\(\s*[""'](?<name>[^""']+)[""']",
        RegexOptions.Compiled);

    private static readonly Regex VariableFunctionRegex = new Regex(
        @"\b(?:SetVariable|GetVariable)\s*\(\s*[""'](?<name>[^""']+)[""']",
        RegexOptions.Compiled);

    private static readonly Regex QuestJournalModeRegex = new Regex(
        @"\bTorisOpenQuestJournal\s*\(\s*[""'](?<mode>[^""']+)[""']",
        RegexOptions.Compiled);

    private static readonly Regex RetiredQuestOffersCommandRegex = new Regex(
        @"\bTorisOpenQuestOffers\s*\(",
        RegexOptions.Compiled);

    [MenuItem("Tools/Toris/Quests/Run Quest Authoring Validation")]
    public static void RunAndLog()
    {
        LogReport(Validate());
    }

    public static PixelCrushersQuestAuthoringReport Validate()
    {
        PixelCrushersQuestAuthoringReport report = new PixelCrushersQuestAuthoringReport();
        QuestAuthoringDatabaseSnapshot database = BuildDatabaseSnapshot(report);
        QuestAuthoringProgressSnapshot progress = ValidateProgressRuleSets(report, database);
        QuestAuthoringRewardSnapshot rewards = ValidateRewardSets(report, database);

        ValidateConventionVariables(report, database, progress);
        ValidateAbandonSets(report, database, progress);
        ValidateCooldownSets(report, database, progress, rewards);
        ValidateQuestJournalInteractables(report, database);
        ValidateDialogueCommandStrings(report, database);

        if (report.Issues.Count == 0)
            report.AddInfo("Quest authoring validation passed with no issues.", null, string.Empty);

        return report;
    }

    public static void LogReport(PixelCrushersQuestAuthoringReport report)
    {
        if (report == null)
            return;

        if (report.Issues.Count == 0)
        {
            Debug.Log("[Quest Authoring Validator] No report was generated.");
            return;
        }

        Debug.Log($"[Quest Authoring Validator] {report.Summary}");

        for (int i = 0; i < report.Issues.Count; i++)
        {
            PixelCrushersQuestAuthoringIssue issue = report.Issues[i];
            string message = string.IsNullOrWhiteSpace(issue.Source)
                ? $"[Quest Authoring Validator] {issue.Message}"
                : $"[Quest Authoring Validator] {issue.Message}\nSource: {issue.Source}";

            switch (issue.Severity)
            {
                case PixelCrushersQuestAuthoringSeverity.Error:
                    Debug.LogError(message, issue.Context);
                    break;
                case PixelCrushersQuestAuthoringSeverity.Warning:
                    Debug.LogWarning(message, issue.Context);
                    break;
                default:
                    Debug.Log(message, issue.Context);
                    break;
            }
        }
    }

    private static QuestAuthoringDatabaseSnapshot BuildDatabaseSnapshot(PixelCrushersQuestAuthoringReport report)
    {
        QuestAuthoringDatabaseSnapshot snapshot = new QuestAuthoringDatabaseSnapshot();
        DialogueDatabase[] databases = LoadAssets<DialogueDatabase>(QuestSearchRoots);

        if (databases.Length == 0)
        {
            report.AddError("No Pixel Crushers DialogueDatabase asset was found under Assets/Scripts/Quest.", null, "Assets/Scripts/Quest");
            return snapshot;
        }

        for (int i = 0; i < databases.Length; i++)
        {
            DialogueDatabase database = databases[i];
            if (database == null)
                continue;

            string databasePath = AssetDatabase.GetAssetPath(database);
            snapshot.DatabasePaths.Add(databasePath);

            if (database.items != null)
                AddQuestsFromDatabase(report, snapshot, database, databasePath);

            if (database.variables != null)
                AddVariablesFromDatabase(report, snapshot, database, databasePath);
        }

        return snapshot;
    }

    private static void AddQuestsFromDatabase(
        PixelCrushersQuestAuthoringReport report,
        QuestAuthoringDatabaseSnapshot snapshot,
        DialogueDatabase database,
        string databasePath)
    {
        for (int i = 0; i < database.items.Count; i++)
        {
            Item item = database.items[i];
            if (item == null || item.IsItem)
                continue;

            string questName = Clean(item.Name);
            if (string.IsNullOrWhiteSpace(questName))
            {
                report.AddWarning("A quest item has an empty Name field.", database, databasePath);
                continue;
            }

            if (snapshot.Quests.ContainsKey(questName))
                report.AddError($"Duplicate Pixel Crushers quest name '{questName}'. Quest names must be globally unique.", database, databasePath);

            QuestAuthoringQuestInfo questInfo = new QuestAuthoringQuestInfo(
                questName,
                Clean(item.Group),
                Mathf.Max(0, item.LookupInt(DialogueSystemFields.EntryCount)),
                databasePath);

            snapshot.Quests[questName] = questInfo;

            if (!string.IsNullOrWhiteSpace(questInfo.Group))
                snapshot.QuestGroups.Add(questInfo.Group);
        }
    }

    private static void AddVariablesFromDatabase(
        PixelCrushersQuestAuthoringReport report,
        QuestAuthoringDatabaseSnapshot snapshot,
        DialogueDatabase database,
        string databasePath)
    {
        for (int i = 0; i < database.variables.Count; i++)
        {
            Variable variable = database.variables[i];
            if (variable == null)
                continue;

            string variableName = Clean(variable.Name);
            if (string.IsNullOrWhiteSpace(variableName))
            {
                report.AddWarning("A Pixel Crushers variable has an empty Name field.", database, databasePath);
                continue;
            }

            if (!snapshot.Variables.Add(variableName))
                report.AddWarning($"Duplicate Pixel Crushers variable name '{variableName}'.", database, databasePath);
        }
    }

    private static QuestAuthoringProgressSnapshot ValidateProgressRuleSets(
        PixelCrushersQuestAuthoringReport report,
        QuestAuthoringDatabaseSnapshot database)
    {
        QuestAuthoringProgressSnapshot snapshot = new QuestAuthoringProgressSnapshot();
        QuestFactProgressRuleSetSO[] ruleSets = LoadAssets<QuestFactProgressRuleSetSO>(QuestSearchRoots);

        for (int i = 0; i < ruleSets.Length; i++)
        {
            QuestFactProgressRuleSetSO ruleSet = ruleSets[i];
            if (ruleSet == null)
                continue;

            string assetPath = AssetDatabase.GetAssetPath(ruleSet);
            List<QuestFactProgressRule> rules = new List<QuestFactProgressRule>();
            ruleSet.AppendRulesTo(rules);

            if (rules.Count == 0)
                report.AddWarning("Progress rule set has no configured rules.", ruleSet, assetPath);

            for (int ruleIndex = 0; ruleIndex < rules.Count; ruleIndex++)
                ValidateProgressRule(report, database, snapshot, ruleSet, assetPath, rules[ruleIndex], ruleIndex);
        }

        return snapshot;
    }

    private static void ValidateProgressRule(
        PixelCrushersQuestAuthoringReport report,
        QuestAuthoringDatabaseSnapshot database,
        QuestAuthoringProgressSnapshot snapshot,
        QuestFactProgressRuleSetSO ruleSet,
        string assetPath,
        QuestFactProgressRule rule,
        int ruleIndex)
    {
        if (rule == null)
            return;

        string source = $"{assetPath} rule #{ruleIndex + 1}";
        string questName = Clean(rule.QuestName);
        string progressVariableName = Clean(rule.ResolvedProgressVariableName);

        if (string.IsNullOrWhiteSpace(questName))
        {
            report.AddError("Progress rule has no Quest Name.", ruleSet, source);
            return;
        }

        ValidateQuestReference(report, database, questName, ruleSet, source);
        ValidateQuestEntryReference(report, database, questName, rule.EntryNumber, ruleSet, source);

        if (rule.RequiredAmount < 1)
            report.AddError($"Progress rule for '{questName}' has Required Amount below 1.", ruleSet, source);

        if (string.IsNullOrWhiteSpace(progressVariableName))
            report.AddError($"Progress rule for '{questName}' resolves to an empty progress variable.", ruleSet, source);
        else
            ValidateVariableReference(report, database, progressVariableName, ruleSet, source, "progress variable");

        if (string.IsNullOrWhiteSpace(rule.ExactId) && string.IsNullOrWhiteSpace(rule.TypeOrTag) && string.IsNullOrWhiteSpace(rule.ContextId))
        {
            report.AddWarning(
                $"Progress rule for '{questName}' matches every '{rule.FactType}' fact because ExactId, TypeOrTag, and ContextId are all empty.",
                ruleSet,
                source);
        }

        snapshot.AddProgressVariable(questName, progressVariableName);
    }

    private static QuestAuthoringRewardSnapshot ValidateRewardSets(
        PixelCrushersQuestAuthoringReport report,
        QuestAuthoringDatabaseSnapshot database)
    {
        QuestAuthoringRewardSnapshot snapshot = new QuestAuthoringRewardSnapshot();
        PixelCrushersQuestRewardSetSO[] rewardSets = LoadAssets<PixelCrushersQuestRewardSetSO>(QuestSearchRoots);

        for (int i = 0; i < rewardSets.Length; i++)
        {
            PixelCrushersQuestRewardSetSO rewardSet = rewardSets[i];
            if (rewardSet == null)
                continue;

            string assetPath = AssetDatabase.GetAssetPath(rewardSet);
            List<PixelCrushersQuestRewardDefinition> rewards = new List<PixelCrushersQuestRewardDefinition>();
            rewardSet.AppendRewardsTo(rewards);

            for (int rewardIndex = 0; rewardIndex < rewards.Count; rewardIndex++)
                ValidateReward(report, database, snapshot, rewardSet, assetPath, rewards[rewardIndex], rewardIndex);
        }

        return snapshot;
    }

    private static void ValidateReward(
        PixelCrushersQuestAuthoringReport report,
        QuestAuthoringDatabaseSnapshot database,
        QuestAuthoringRewardSnapshot snapshot,
        PixelCrushersQuestRewardSetSO rewardSet,
        string assetPath,
        PixelCrushersQuestRewardDefinition reward,
        int rewardIndex)
    {
        if (reward == null)
            return;

        string source = $"{assetPath} reward #{rewardIndex + 1}";
        string questName = Clean(reward.QuestName);

        if (string.IsNullOrWhiteSpace(questName))
        {
            report.AddError("Reward entry has no Quest Name.", rewardSet, source);
            return;
        }

        ValidateQuestReference(report, database, questName, rewardSet, source);

        if (!snapshot.RewardQuestNames.Add(questName))
            report.AddWarning($"Quest '{questName}' has more than one reward definition.", rewardSet, source);

        snapshot.RewardGrantedVariables[questName] = reward.ResolvedRewardGrantedVariableName;

        if (reward.GoldReward <= 0 && reward.ExperienceReward <= 0 && reward.ItemReward == null)
            report.AddWarning($"Reward entry for '{questName}' does not grant gold, XP, or an item.", rewardSet, source);

        if (reward.ItemReward != null)
        {
            if (reward.ItemRewardQuantity < 1)
                report.AddError($"Item reward for '{questName}' has quantity below 1.", rewardSet, source);
        }
    }

    private static void ValidateAbandonSets(
        PixelCrushersQuestAuthoringReport report,
        QuestAuthoringDatabaseSnapshot database,
        QuestAuthoringProgressSnapshot progress)
    {
        PixelCrushersQuestAbandonSetSO[] abandonSets = LoadAssets<PixelCrushersQuestAbandonSetSO>(QuestSearchRoots);
        HashSet<string> seenQuestNames = new HashSet<string>(StringComparer.Ordinal);

        for (int i = 0; i < abandonSets.Length; i++)
        {
            PixelCrushersQuestAbandonSetSO abandonSet = abandonSets[i];
            if (abandonSet == null)
                continue;

            string assetPath = AssetDatabase.GetAssetPath(abandonSet);
            List<PixelCrushersQuestAbandonDefinition> abandons = new List<PixelCrushersQuestAbandonDefinition>();
            abandonSet.AppendAbandonsTo(abandons);

            for (int abandonIndex = 0; abandonIndex < abandons.Count; abandonIndex++)
            {
                PixelCrushersQuestAbandonDefinition abandon = abandons[abandonIndex];
                if (abandon == null)
                    continue;

                string source = $"{assetPath} abandon #{abandonIndex + 1}";
                string questName = Clean(abandon.QuestName);

                if (string.IsNullOrWhiteSpace(questName))
                {
                    report.AddError("Abandon entry has no Quest Name.", abandonSet, source);
                    continue;
                }

                ValidateQuestReference(report, database, questName, abandonSet, source);

                if (!seenQuestNames.Add(questName))
                    report.AddWarning($"Quest '{questName}' has more than one abandon definition.", abandonSet, source);

                ValidateProgressResetCoverage(report, database, progress, abandonSet, source, questName, abandon.ProgressVariableNamesToReset, "abandon");
            }
        }
    }

    private static void ValidateCooldownSets(
        PixelCrushersQuestAuthoringReport report,
        QuestAuthoringDatabaseSnapshot database,
        QuestAuthoringProgressSnapshot progress,
        QuestAuthoringRewardSnapshot rewards)
    {
        PixelCrushersRepeatableQuestCooldownSetSO[] cooldownSets = LoadAssets<PixelCrushersRepeatableQuestCooldownSetSO>(QuestSearchRoots);
        HashSet<string> seenQuestNames = new HashSet<string>(StringComparer.Ordinal);

        for (int i = 0; i < cooldownSets.Length; i++)
        {
            PixelCrushersRepeatableQuestCooldownSetSO cooldownSet = cooldownSets[i];
            if (cooldownSet == null)
                continue;

            string assetPath = AssetDatabase.GetAssetPath(cooldownSet);
            List<PixelCrushersRepeatableQuestCooldownDefinition> cooldowns = new List<PixelCrushersRepeatableQuestCooldownDefinition>();
            cooldownSet.AppendCooldownsTo(cooldowns);

            for (int cooldownIndex = 0; cooldownIndex < cooldowns.Count; cooldownIndex++)
            {
                PixelCrushersRepeatableQuestCooldownDefinition cooldown = cooldowns[cooldownIndex];
                if (cooldown == null)
                    continue;

                string source = $"{assetPath} cooldown #{cooldownIndex + 1}";
                string questName = Clean(cooldown.QuestName);

                if (string.IsNullOrWhiteSpace(questName))
                {
                    report.AddError("Cooldown entry has no Quest Name.", cooldownSet, source);
                    continue;
                }

                ValidateQuestReference(report, database, questName, cooldownSet, source);

                if (!seenQuestNames.Add(questName))
                    report.AddWarning($"Quest '{questName}' has more than one cooldown definition.", cooldownSet, source);

                ValidateProgressResetCoverage(report, database, progress, cooldownSet, source, questName, cooldown.ProgressVariableNamesToReset, "cooldown");

                string rewardVariable = string.Empty;
                if (rewards.RewardGrantedVariables.TryGetValue(questName, out rewardVariable)
                    && !string.Equals(rewardVariable, cooldown.ResolvedRewardGrantedVariableNameToReset, StringComparison.Ordinal))
                {
                    report.AddWarning(
                        $"Cooldown for '{questName}' resets reward guard '{cooldown.ResolvedRewardGrantedVariableNameToReset}', but the reward definition uses '{rewardVariable}'.",
                        cooldownSet,
                        source);
                }
            }
        }
    }

    private static void ValidateConventionVariables(
        PixelCrushersQuestAuthoringReport report,
        QuestAuthoringDatabaseSnapshot database,
        QuestAuthoringProgressSnapshot progress)
    {
        foreach (string variableName in database.Variables)
        {
            foreach (QuestAuthoringQuestInfo questInfo in database.Quests.Values)
            {
                if (TryValidateConventionVariable(report, variableName, questInfo))
                {
                    progress.AddProgressVariable(questInfo.Name, variableName);
                    break;
                }
            }
        }
    }

    private static bool TryValidateConventionVariable(
        PixelCrushersQuestAuthoringReport report,
        string variableName,
        QuestAuthoringQuestInfo questInfo)
    {
        string questSegment = PixelCrushersQuestNaming.SanitizeSegment(questInfo.Name);
        if (string.IsNullOrWhiteSpace(questSegment))
            return false;

        string questPrefix = $"{questSegment}_";
        if (!variableName.StartsWith(questPrefix, StringComparison.Ordinal))
            return false;

        string remaining = variableName.Substring(questPrefix.Length);
        foreach (QuestFactType factType in Enum.GetValues(typeof(QuestFactType)))
        {
            if (factType == QuestFactType.None)
                continue;

            string factPrefix = $"{factType}_";
            if (!remaining.StartsWith(factPrefix, StringComparison.Ordinal))
                continue;

            ValidateConventionVariableTargetAndRequirement(report, variableName, remaining.Substring(factPrefix.Length), questInfo);
            return true;
        }

        return false;
    }

    private static void ValidateConventionVariableTargetAndRequirement(
        PixelCrushersQuestAuthoringReport report,
        string variableName,
        string targetAndRequirement,
        QuestAuthoringQuestInfo questInfo)
    {
        string targetSegment = targetAndRequirement;
        string requiredMarker = $"_{PixelCrushersQuestNaming.RequiredAmountSegment}_";
        int requiredMarkerIndex = targetAndRequirement.LastIndexOf(requiredMarker, StringComparison.Ordinal);
        if (requiredMarkerIndex >= 0)
        {
            string requiredAmountText = targetAndRequirement.Substring(requiredMarkerIndex + requiredMarker.Length);
            int requiredAmount;
            if (!int.TryParse(requiredAmountText, out requiredAmount) || requiredAmount < 1)
            {
                report.AddError(
                    $"Convention progress variable '{variableName}' has an invalid required amount suffix. Use _Required_# with a number above 0.",
                    null,
                    $"{questInfo.SourcePath} convention variable");
            }

            targetSegment = targetAndRequirement.Substring(0, requiredMarkerIndex);
        }

        if (string.IsNullOrWhiteSpace(PixelCrushersQuestNaming.SanitizeSegment(targetSegment)))
        {
            report.AddError(
                $"Convention progress variable '{variableName}' has no target segment after the fact type.",
                null,
                $"{questInfo.SourcePath} convention variable");
        }
    }

    private static void ValidateQuestJournalInteractables(
        PixelCrushersQuestAuthoringReport report,
        QuestAuthoringDatabaseSnapshot database)
    {
        GameObject[] prefabs = LoadPrefabAssets(QuestSearchRoots);

        for (int i = 0; i < prefabs.Length; i++)
        {
            GameObject prefab = prefabs[i];
            if (prefab == null)
                continue;

            string prefabPath = AssetDatabase.GetAssetPath(prefab);
            PixelCrushersQuestJournalInteractable[] interactables = prefab.GetComponentsInChildren<PixelCrushersQuestJournalInteractable>(true);
            for (int interactableIndex = 0; interactableIndex < interactables.Length; interactableIndex++)
                ValidateQuestJournalInteractable(report, database, interactables[interactableIndex], prefabPath);
        }
    }

    private static void ValidateQuestJournalInteractable(
        PixelCrushersQuestAuthoringReport report,
        QuestAuthoringDatabaseSnapshot database,
        PixelCrushersQuestJournalInteractable interactable,
        string prefabPath)
    {
        if (interactable == null)
            return;

        SerializedObject serializedObject = new SerializedObject(interactable);
        string journalMode = Clean(serializedObject.FindProperty("_journalMode")?.stringValue);
        ValidateJournalMode(report, database, journalMode, interactable, prefabPath);

        SerializedProperty questsProperty = serializedObject.FindProperty("_questsToMarkGrantable");
        if (questsProperty == null || !questsProperty.isArray)
            return;

        for (int questIndex = 0; questIndex < questsProperty.arraySize; questIndex++)
        {
            string questName = Clean(questsProperty.GetArrayElementAtIndex(questIndex).stringValue);
            if (string.IsNullOrWhiteSpace(questName))
                continue;

            ValidateQuestReference(report, database, questName, interactable, $"{prefabPath} grantable quest #{questIndex + 1}");
        }
    }

    private static void ValidateDialogueCommandStrings(
        PixelCrushersQuestAuthoringReport report,
        QuestAuthoringDatabaseSnapshot database)
    {
        for (int i = 0; i < database.DatabasePaths.Count; i++)
        {
            string databasePath = database.DatabasePaths[i];
            string absolutePath = Path.Combine(Application.dataPath, databasePath.Substring("Assets/".Length));
            if (!File.Exists(absolutePath))
                continue;

            string text = File.ReadAllText(absolutePath);

            ValidateRegexQuestReferences(report, database, databasePath, text);
            ValidateRegexVariableReferences(report, database, databasePath, text);
            ValidateRegexJournalModes(report, database, databasePath, text);
            ValidateRetiredQuestOfferCommands(report, databasePath, text);
        }
    }

    private static void ValidateRegexQuestReferences(
        PixelCrushersQuestAuthoringReport report,
        QuestAuthoringDatabaseSnapshot database,
        string databasePath,
        string text)
    {
        foreach (Match match in QuestFunctionRegex.Matches(text))
        {
            string questName = Clean(match.Groups["name"].Value);
            if (!string.IsNullOrWhiteSpace(questName))
                ValidateQuestReference(report, database, questName, null, $"{databasePath} Lua quest reference");
        }
    }

    private static void ValidateRegexVariableReferences(
        PixelCrushersQuestAuthoringReport report,
        QuestAuthoringDatabaseSnapshot database,
        string databasePath,
        string text)
    {
        foreach (Match match in VariableFunctionRegex.Matches(text))
        {
            string variableName = Clean(match.Groups["name"].Value);
            if (!string.IsNullOrWhiteSpace(variableName))
                ValidateVariableReference(report, database, variableName, null, $"{databasePath} Lua variable reference", "Lua variable");
        }
    }

    private static void ValidateRegexJournalModes(
        PixelCrushersQuestAuthoringReport report,
        QuestAuthoringDatabaseSnapshot database,
        string databasePath,
        string text)
    {
        foreach (Match match in QuestJournalModeRegex.Matches(text))
            ValidateJournalMode(report, database, Clean(match.Groups["mode"].Value), null, $"{databasePath} Lua quest journal command");
    }

    private static void ValidateRetiredQuestOfferCommands(
        PixelCrushersQuestAuthoringReport report,
        string databasePath,
        string text)
    {
        MatchCollection matches = RetiredQuestOffersCommandRegex.Matches(text);
        for (int i = 0; i < matches.Count; i++)
        {
            report.AddError(
                "Retired Lua command 'TorisOpenQuestOffers' is still referenced. Use TorisOpenQuestJournal(\"Available:GroupName\") instead.",
                null,
                $"{databasePath} retired Lua command #{i + 1}");
        }
    }

    private static void ValidateJournalMode(
        PixelCrushersQuestAuthoringReport report,
        QuestAuthoringDatabaseSnapshot database,
        string journalMode,
        UnityEngine.Object context,
        string source)
    {
        if (string.IsNullOrWhiteSpace(journalMode))
        {
            report.AddWarning("Quest journal mode is empty. It will fall back to Active mode.", context, source);
            return;
        }

        string groupFilter = ExtractJournalGroupFilter(journalMode);
        if (string.IsNullOrWhiteSpace(groupFilter) || string.Equals(groupFilter, "All", StringComparison.Ordinal))
            return;

        ValidateQuestGroupReference(report, database, groupFilter, context, source);
    }

    private static void ValidateQuestReference(
        PixelCrushersQuestAuthoringReport report,
        QuestAuthoringDatabaseSnapshot database,
        string questName,
        UnityEngine.Object context,
        string source)
    {
        if (string.IsNullOrWhiteSpace(questName))
        {
            report.AddError("Quest reference is empty.", context, source);
            return;
        }

        if (!database.Quests.ContainsKey(questName))
            report.AddError($"Unknown Pixel Crushers quest '{questName}'.", context, source);
    }

    private static void ValidateQuestEntryReference(
        PixelCrushersQuestAuthoringReport report,
        QuestAuthoringDatabaseSnapshot database,
        string questName,
        int entryNumber,
        UnityEngine.Object context,
        string source)
    {
        if (entryNumber <= 0)
            return;

        QuestAuthoringQuestInfo questInfo = null;
        if (!database.Quests.TryGetValue(questName, out questInfo))
            return;

        if (questInfo.EntryCount <= 0)
        {
            report.AddWarning($"Quest '{questName}' references entry {entryNumber}, but the quest has no entries.", context, source);
            return;
        }

        if (entryNumber > questInfo.EntryCount)
            report.AddError($"Quest '{questName}' references entry {entryNumber}, but only has {questInfo.EntryCount} entr{(questInfo.EntryCount == 1 ? "y" : "ies")}.", context, source);
    }

    private static void ValidateQuestGroupReference(
        PixelCrushersQuestAuthoringReport report,
        QuestAuthoringDatabaseSnapshot database,
        string groupId,
        UnityEngine.Object context,
        string source)
    {
        if (string.IsNullOrWhiteSpace(groupId))
            return;

        if (!database.QuestGroups.Contains(groupId))
            report.AddWarning($"Quest group '{groupId}' is not used by any Pixel Crushers quest.", context, source);
    }

    private static void ValidateVariableReference(
        PixelCrushersQuestAuthoringReport report,
        QuestAuthoringDatabaseSnapshot database,
        string variableName,
        UnityEngine.Object context,
        string source,
        string label)
    {
        if (string.IsNullOrWhiteSpace(variableName))
        {
            report.AddWarning($"A {label} is empty.", context, source);
            return;
        }

        if (!database.Variables.Contains(variableName))
            report.AddWarning($"Pixel Crushers {label} '{variableName}' is not declared in the dialogue database.", context, source);
    }

    private static void ValidateProgressResetCoverage(
        PixelCrushersQuestAuthoringReport report,
        QuestAuthoringDatabaseSnapshot database,
        QuestAuthoringProgressSnapshot progress,
        UnityEngine.Object context,
        string source,
        string questName,
        string[] resetVariables,
        string resetReason)
    {
        HashSet<string> variablesForQuest = progress.GetVariablesForQuest(questName);
        if (variablesForQuest.Count == 0)
            return;

        HashSet<string> resetVariableSet = new HashSet<string>(StringComparer.Ordinal);
        if (resetVariables != null)
        {
            for (int i = 0; i < resetVariables.Length; i++)
            {
                string resetVariable = Clean(resetVariables[i]);
                if (string.IsNullOrWhiteSpace(resetVariable))
                    continue;

                resetVariableSet.Add(resetVariable);
                ValidateVariableReference(report, database, resetVariable, context, source, $"{resetReason} reset variable");
            }
        }

        foreach (string progressVariable in variablesForQuest)
        {
            if (!resetVariableSet.Contains(progressVariable))
            {
                report.AddWarning(
                    $"Quest '{questName}' has progress variable '{progressVariable}', but the {resetReason} entry does not reset it.",
                    context,
                    source);
            }
        }
    }

    private static string ExtractJournalGroupFilter(string journalMode)
    {
        if (string.IsNullOrWhiteSpace(journalMode))
            return string.Empty;

        int separatorIndex = journalMode.IndexOf(':');
        if (separatorIndex < 0)
            separatorIndex = journalMode.IndexOf('|');

        if (separatorIndex < 0 || separatorIndex >= journalMode.Length - 1)
            return string.Empty;

        return Clean(journalMode.Substring(separatorIndex + 1));
    }

    private static T[] LoadAssets<T>(string[] searchRoots) where T : UnityEngine.Object
    {
        string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", searchRoots);
        List<T> assets = new List<T>(guids.Length);

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                assets.Add(asset);
        }

        return assets.ToArray();
    }

    private static GameObject[] LoadPrefabAssets(string[] searchRoots)
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", searchRoots);
        List<GameObject> prefabs = new List<GameObject>(guids.Length);

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
                prefabs.Add(prefab);
        }

        return prefabs.ToArray();
    }

    private static string Clean(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}

public sealed class PixelCrushersQuestAuthoringReport
{
    private readonly List<PixelCrushersQuestAuthoringIssue> _issues = new List<PixelCrushersQuestAuthoringIssue>();

    public IReadOnlyList<PixelCrushersQuestAuthoringIssue> Issues => _issues;

    public string Summary
    {
        get
        {
            int errorCount = Count(PixelCrushersQuestAuthoringSeverity.Error);
            int warningCount = Count(PixelCrushersQuestAuthoringSeverity.Warning);
            int infoCount = Count(PixelCrushersQuestAuthoringSeverity.Info);
            return $"{errorCount} error(s), {warningCount} warning(s), {infoCount} info item(s)";
        }
    }

    public void AddError(string message, UnityEngine.Object context, string source)
    {
        Add(PixelCrushersQuestAuthoringSeverity.Error, message, context, source);
    }

    public void AddWarning(string message, UnityEngine.Object context, string source)
    {
        Add(PixelCrushersQuestAuthoringSeverity.Warning, message, context, source);
    }

    public void AddInfo(string message, UnityEngine.Object context, string source)
    {
        Add(PixelCrushersQuestAuthoringSeverity.Info, message, context, source);
    }

    private void Add(PixelCrushersQuestAuthoringSeverity severity, string message, UnityEngine.Object context, string source)
    {
        _issues.Add(new PixelCrushersQuestAuthoringIssue(severity, message, context, source));
    }

    private int Count(PixelCrushersQuestAuthoringSeverity severity)
    {
        int count = 0;
        for (int i = 0; i < _issues.Count; i++)
        {
            if (_issues[i].Severity == severity)
                count++;
        }

        return count;
    }
}

public sealed class PixelCrushersQuestAuthoringIssue
{
    public PixelCrushersQuestAuthoringIssue(
        PixelCrushersQuestAuthoringSeverity severity,
        string message,
        UnityEngine.Object context,
        string source)
    {
        Severity = severity;
        Message = message;
        Context = context;
        Source = source;
    }

    public PixelCrushersQuestAuthoringSeverity Severity { get; }
    public string Message { get; }
    public UnityEngine.Object Context { get; }
    public string Source { get; }
}

public enum PixelCrushersQuestAuthoringSeverity
{
    Info,
    Warning,
    Error
}

internal sealed class QuestAuthoringDatabaseSnapshot
{
    public readonly Dictionary<string, QuestAuthoringQuestInfo> Quests = new Dictionary<string, QuestAuthoringQuestInfo>(StringComparer.Ordinal);
    public readonly HashSet<string> Variables = new HashSet<string>(StringComparer.Ordinal);
    public readonly HashSet<string> QuestGroups = new HashSet<string>(StringComparer.Ordinal);
    public readonly List<string> DatabasePaths = new List<string>();
}

internal sealed class QuestAuthoringQuestInfo
{
    public QuestAuthoringQuestInfo(string name, string group, int entryCount, string sourcePath)
    {
        Name = name;
        Group = group;
        EntryCount = entryCount;
        SourcePath = sourcePath;
    }

    public string Name { get; }
    public string Group { get; }
    public int EntryCount { get; }
    public string SourcePath { get; }
}

internal sealed class QuestAuthoringProgressSnapshot
{
    private readonly Dictionary<string, HashSet<string>> _variablesByQuest = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

    public void AddProgressVariable(string questName, string variableName)
    {
        if (string.IsNullOrWhiteSpace(questName) || string.IsNullOrWhiteSpace(variableName))
            return;

        HashSet<string> variables = null;
        if (!_variablesByQuest.TryGetValue(questName, out variables))
        {
            variables = new HashSet<string>(StringComparer.Ordinal);
            _variablesByQuest.Add(questName, variables);
        }

        variables.Add(variableName);
    }

    public HashSet<string> GetVariablesForQuest(string questName)
    {
        HashSet<string> variables = null;
        if (string.IsNullOrWhiteSpace(questName) || !_variablesByQuest.TryGetValue(questName, out variables))
            return new HashSet<string>(StringComparer.Ordinal);

        return variables;
    }
}

internal sealed class QuestAuthoringRewardSnapshot
{
    public readonly HashSet<string> RewardQuestNames = new HashSet<string>(StringComparer.Ordinal);
    public readonly Dictionary<string, string> RewardGrantedVariables = new Dictionary<string, string>(StringComparer.Ordinal);
}
#endif
