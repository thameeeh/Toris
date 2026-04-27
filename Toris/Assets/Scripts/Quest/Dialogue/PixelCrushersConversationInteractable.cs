using System;
using PixelCrushers.DialogueSystem;
using UnityEngine;

/// <summary>
/// Toris-owned NPC interaction entry point for Pixel Crushers conversations.
/// It keeps the game's normal interact flow intact while letting Dialogue System own
/// the actual conversation and quest state.
/// Add this to an NPC root, assign a default conversation, then optionally add ordered
/// quest routes when the same NPC needs different conversations for different quest states.
/// </summary>
public class PixelCrushersConversationInteractable : MonoBehaviour, IInteractable
{
    [Header("Conversation Selection")]
    [Tooltip("Conversation title to start when no quest route resolves. This must exactly match a Pixel Crushers conversation title.")]
    [SerializeField] private string _defaultConversation = "Guide_Intro";
    [Tooltip("Quest-state routes checked from top to bottom. Put newer or higher-priority story beats before older completed beats.")]
    [SerializeField] private PixelCrushersConversationRoute[] _questConversationRoutes = Array.Empty<PixelCrushersConversationRoute>();

    [Header("Quest Fact Reporting")]
    [Tooltip("Enable if talking to this NPC should count toward quests, such as 'Talk to the Smith'.")]
    [SerializeField] private bool _reportInteractNpcFact;
    [Tooltip("Stable exact NPC ID reported to quest rules. Example: SmithNPC. Do not rely on GameObject names.")]
    [SerializeField] private string _npcFactId = string.Empty;
    [Tooltip("Optional NPC group/type reported to quest rules. Example: Smith, Guide, Shopkeeper.")]
    [SerializeField] private string _npcFactTypeOrTag = string.Empty;
    [Tooltip("Optional place or story context for this NPC interaction. Example: MainArea.")]
    [SerializeField] private string _npcFactContextId = string.Empty;

    [Header("Debug")]
    [Tooltip("Logs which conversation was selected and why. Editor only.")]
    [SerializeField] private bool _debugConversationSelection = true;

    public void Interact(GameObject interactor)
    {
        if (interactor == null)
            return;

        string conversation = ResolveConversationTitle(out string stateLabel);
        if (string.IsNullOrWhiteSpace(conversation))
        {
#if UNITY_EDITOR
            if (_debugConversationSelection)
                Debug.LogWarning("[PixelCrushersConversationInteractable] No conversation title resolved.", this);
#endif
            return;
        }

#if UNITY_EDITOR
        if (!PixelCrushersQuestBridge.HasDialogueManager)
        {
            if (_debugConversationSelection)
                Debug.LogWarning("[PixelCrushersConversationInteractable] No Dialogue Manager instance is active in the scene.", this);
            return;
        }
#endif

#if UNITY_EDITOR
        if (_debugConversationSelection)
        {
            Debug.Log($"[PixelCrushersConversationInteractable] Starting '{conversation}' for state '{stateLabel}'.", this);
        }
#endif

        PixelCrushersQuestBridge.StartConversation(conversation, interactor.transform, transform);
        ReportNpcInteractionFactIfNeeded();
    }

    private string ResolveConversationTitle(out string stateLabel)
    {
        if (TryResolveConfiguredRoute(out string routedConversation, out stateLabel))
            return routedConversation;

        stateLabel = "default";
        return ResolveDefaultConversation();
    }

    private bool TryResolveConfiguredRoute(out string conversation, out string stateLabel)
    {
        conversation = string.Empty;
        stateLabel = "no quest context";

        if (_questConversationRoutes == null)
            return false;

        for (int i = 0; i < _questConversationRoutes.Length; i++)
        {
            PixelCrushersConversationRoute route = _questConversationRoutes[i];
            if (route == null || !route.TryResolve(out conversation, out stateLabel))
                continue;

            return true;
        }

        return false;
    }

    private string ResolveDefaultConversation()
    {
        return string.IsNullOrWhiteSpace(_defaultConversation) ? string.Empty : _defaultConversation;
    }

    private void ReportNpcInteractionFactIfNeeded()
    {
        if (!_reportInteractNpcFact)
            return;

        if (string.IsNullOrWhiteSpace(_npcFactId) && string.IsNullOrWhiteSpace(_npcFactTypeOrTag))
            return;

        PixelCrushersQuestFactReporter.Report(
            QuestFact.InteractNpc(_npcFactId, _npcFactTypeOrTag, 1, _npcFactContextId));
    }
}

[Serializable]
public class PixelCrushersConversationRoute
{
    [Tooltip("Pixel Crushers quest name this route watches. Must exactly match the quest name in the dialogue database.")]
    public string QuestName = string.Empty;
    [Tooltip("Conversation title used when the quest is unassigned or grantable. Leave blank if this route should not handle that state.")]
    public string ConversationWhenUnassigned = string.Empty;
    [Tooltip("Conversation title used while the quest is active. Usually reminder or in-progress dialogue.")]
    public string ConversationWhenActive = string.Empty;
    [Tooltip("Conversation title used when the quest is ready to turn in.")]
    public string ConversationWhenReturnToNpc = string.Empty;
    [Tooltip("Conversation title used after the quest succeeds or is done.")]
    public string ConversationWhenSuccess = string.Empty;

    public bool TryResolve(out string conversation, out string stateLabel)
    {
        conversation = string.Empty;
        stateLabel = "no quest context";

        if (string.IsNullOrWhiteSpace(QuestName))
            return false;

        string questState = PixelCrushersQuestBridge.GetQuestStateString(QuestName);
        stateLabel = $"{QuestName}:{questState}";

        conversation = ResolveConversationForState(questState);
        return !string.IsNullOrWhiteSpace(conversation);
    }

    private string ResolveConversationForState(string questState)
    {
        switch (questState)
        {
            case QuestLog.ActiveStateString:
                return ConversationWhenActive;

            case QuestLog.ReturnToNPCStateString:
                return ConversationWhenReturnToNpc;

            case QuestLog.SuccessStateString:
            case QuestLog.DoneStateString:
                return ConversationWhenSuccess;

            case QuestLog.UnassignedStateString:
            case QuestLog.GrantableStateString:
            default:
                return ConversationWhenUnassigned;
        }
    }
}
