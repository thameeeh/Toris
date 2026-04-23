using PixelCrushers.DialogueSystem;
using UnityEngine;

/// <summary>
/// Thin Toris-side helper around Pixel Crushers quest and variable APIs.
/// This is not a second quest system. It only gives Toris gameplay code one small place
/// to talk to the Dialogue System without scattering direct QuestLog / DialogueLua calls everywhere.
/// </summary>
public static class PixelCrushersQuestBridge
{
    private const string KillThreeLeaderWolvesQuestName = "Kill_3_Leader_Wolves";
    private const string LeaderWolfEnemyId = "LeaderWolf";
    private const string LeaderWolfKillsVariableName = "LeaderWolfKills";
    private const int KillThreeLeaderWolvesEntryNumber = 1;
    private const int RequiredLeaderWolfKills = 3;

    public static bool HasDialogueManager => DialogueManager.hasInstance;

    public static string UnassignedState => QuestLog.UnassignedStateString;
    public static string ActiveState => QuestLog.ActiveStateString;
    public static string SuccessState => QuestLog.SuccessStateString;
    public static string DoneState => QuestLog.DoneStateString;
    public static string ReturnToNpcState => QuestLog.ReturnToNPCStateString;

    public static string GetQuestStateString(string questName)
    {
        if (string.IsNullOrWhiteSpace(questName))
            return UnassignedState;

        return QuestLog.CurrentQuestState(questName);
    }

    public static QuestState GetQuestState(string questName)
    {
        if (string.IsNullOrWhiteSpace(questName))
            return QuestState.Unassigned;

        return QuestLog.GetQuestState(questName);
    }

    public static void SetQuestState(string questName, string state)
    {
        if (string.IsNullOrWhiteSpace(questName) || string.IsNullOrWhiteSpace(state))
            return;

        QuestLog.SetQuestState(questName, state);
        SendTrackerRefreshIfReady();
    }

    public static void SetQuestState(string questName, QuestState state)
    {
        if (string.IsNullOrWhiteSpace(questName))
            return;

        QuestLog.SetQuestState(questName, state);
        SendTrackerRefreshIfReady();
    }

    public static void StartQuest(string questName)
    {
        if (string.IsNullOrWhiteSpace(questName))
            return;

        QuestLog.SetQuestState(questName, QuestState.Active);
        SendTrackerRefreshIfReady();
    }

    public static void SetQuestEntryState(string questName, int entryNumber, QuestState state)
    {
        if (string.IsNullOrWhiteSpace(questName) || entryNumber < 1)
            return;

        QuestLog.SetQuestEntryState(questName, entryNumber, state);
        SendTrackerRefreshIfReady();
    }

    public static void SetQuestEntryState(string questName, int entryNumber, string state)
    {
        if (string.IsNullOrWhiteSpace(questName) || entryNumber < 1 || string.IsNullOrWhiteSpace(state))
            return;

        QuestLog.SetQuestEntryState(questName, entryNumber, state);
        SendTrackerRefreshIfReady();
    }

    public static int GetIntVariable(string variableName, int defaultValue = 0)
    {
        if (string.IsNullOrWhiteSpace(variableName))
            return defaultValue;

        return DialogueLua.GetVariable(variableName, defaultValue);
    }

    public static void SetIntVariable(string variableName, int value)
    {
        if (string.IsNullOrWhiteSpace(variableName))
            return;

        DialogueLua.SetVariable(variableName, value);
        SendTrackerRefreshIfReady();
    }

    public static int IncrementIntVariable(string variableName, int amount = 1)
    {
        int nextValue = GetIntVariable(variableName) + amount;
        SetIntVariable(variableName, nextValue);
        return nextValue;
    }

    // Gameplay reports facts such as "LeaderWolf died", and the bridge maps those facts
    // to the current Pixel Crushers quest state for the first tutorial quest slice.
    public static void ReportEnemyKilled(string enemyId)
    {
        if (string.IsNullOrWhiteSpace(enemyId) || !HasDialogueManager)
            return;

        if (!string.Equals(enemyId, LeaderWolfEnemyId, System.StringComparison.Ordinal))
            return;

        if (GetQuestState(KillThreeLeaderWolvesQuestName) != QuestState.Active)
            return;

        int nextKillCount = IncrementIntVariable(LeaderWolfKillsVariableName);

#if UNITY_EDITOR
        LogDebug($"Reported enemy kill '{enemyId}'. {LeaderWolfKillsVariableName}={nextKillCount}.");
#endif

        if (nextKillCount < RequiredLeaderWolfKills)
            return;

        SetIntVariable(LeaderWolfKillsVariableName, RequiredLeaderWolfKills);
        SetQuestEntryState(KillThreeLeaderWolvesQuestName, KillThreeLeaderWolvesEntryNumber, QuestState.Success);
        SetQuestState(KillThreeLeaderWolvesQuestName, QuestState.ReturnToNPC);

#if UNITY_EDITOR
        LogDebug($"Quest '{KillThreeLeaderWolvesQuestName}' reached turn-in after {RequiredLeaderWolfKills} {LeaderWolfEnemyId} kills.");
#endif
    }

    public static void StartConversation(string conversationTitle, Transform actor, Transform conversant)
    {
        if (string.IsNullOrWhiteSpace(conversationTitle))
            return;

        DialogueManager.StartConversation(conversationTitle, actor, conversant);
    }

    private static void SendTrackerRefreshIfReady()
    {
        if (!HasDialogueManager)
            return;

        DialogueManager.SendUpdateTracker();
    }

#if UNITY_EDITOR
    public static void LogDebug(string message, Object context = null)
    {
        Debug.Log($"[PixelCrushersQuestBridge] {message}", context);
    }
#endif
}
