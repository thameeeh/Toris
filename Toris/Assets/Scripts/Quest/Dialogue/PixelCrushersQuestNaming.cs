using System.Text;

/// <summary>
/// Shared naming conventions for Toris glue variables stored in Pixel Crushers Lua.
/// Manual inspector fields can still override these names for special-case quests.
/// </summary>
public static class PixelCrushersQuestNaming
{
    private const string EmptyTargetSegment = "Any";

    public static string RewardGrantedVariable(string questName)
    {
        string safeQuestName = SanitizeSegment(questName);
        return string.IsNullOrWhiteSpace(safeQuestName) ? string.Empty : $"{safeQuestName}_RewardsGranted";
    }

    public static string RewardGoldGrantedVariable(string rewardGrantedVariableName)
    {
        return AppendSuffix(rewardGrantedVariableName, "Gold");
    }

    public static string RewardExperienceGrantedVariable(string rewardGrantedVariableName)
    {
        return AppendSuffix(rewardGrantedVariableName, "Experience");
    }

    public static string RewardItemGrantedVariable(string rewardGrantedVariableName)
    {
        return AppendSuffix(rewardGrantedVariableName, "Item");
    }

    public static string CooldownEndVariable(string questName)
    {
        string safeQuestName = SanitizeSegment(questName);
        return string.IsNullOrWhiteSpace(safeQuestName) ? string.Empty : $"{safeQuestName}_CooldownEndsAtUtc";
    }

    public static string CompletionCountVariable(string questName)
    {
        string safeQuestName = SanitizeSegment(questName);
        return string.IsNullOrWhiteSpace(safeQuestName) ? string.Empty : $"{safeQuestName}_CompletionCount";
    }

    public static string AbandonCountVariable(string questName)
    {
        string safeQuestName = SanitizeSegment(questName);
        return string.IsNullOrWhiteSpace(safeQuestName) ? string.Empty : $"{safeQuestName}_AbandonCount";
    }

    public static string ProgressVariable(string questName, QuestFactType factType, string exactId, string typeOrTag, string contextId)
    {
        string safeQuestName = SanitizeSegment(questName);
        if (string.IsNullOrWhiteSpace(safeQuestName))
            return string.Empty;

        string targetSegment = FirstNonEmptySegment(exactId, typeOrTag, contextId);
        return $"{safeQuestName}_{factType}_{targetSegment}";
    }

    public static string SanitizeSegment(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        string trimmedValue = value.Trim();
        StringBuilder builder = new StringBuilder(trimmedValue.Length);
        bool previousWasUnderscore = false;

        for (int i = 0; i < trimmedValue.Length; i++)
        {
            char character = trimmedValue[i];
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
                previousWasUnderscore = false;
                continue;
            }

            if (character == '_' && builder.Length > 0 && !previousWasUnderscore)
            {
                builder.Append(character);
                previousWasUnderscore = true;
            }
        }

        return builder.ToString().Trim('_');
    }

    private static string AppendSuffix(string baseVariableName, string suffix)
    {
        string safeBaseVariableName = SanitizeSegment(baseVariableName);
        string safeSuffix = SanitizeSegment(suffix);

        if (string.IsNullOrWhiteSpace(safeBaseVariableName) || string.IsNullOrWhiteSpace(safeSuffix))
            return string.Empty;

        return $"{safeBaseVariableName}_{safeSuffix}";
    }

    private static string FirstNonEmptySegment(string exactId, string typeOrTag, string contextId)
    {
        string exactSegment = SanitizeSegment(exactId);
        if (!string.IsNullOrWhiteSpace(exactSegment))
            return exactSegment;

        string typeSegment = SanitizeSegment(typeOrTag);
        if (!string.IsNullOrWhiteSpace(typeSegment))
            return typeSegment;

        string contextSegment = SanitizeSegment(contextId);
        if (!string.IsNullOrWhiteSpace(contextSegment))
            return contextSegment;

        return EmptyTargetSegment;
    }
}
