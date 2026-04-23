using PixelCrushers.DialogueSystem;
using UnityEngine;

/// <summary>
/// Toris-owned NPC interaction entry point for Pixel Crushers conversations.
/// It keeps the game's normal interact flow intact while letting Dialogue System own
/// the actual conversation and quest state.
/// </summary>
public class PixelCrushersConversationInteractable : MonoBehaviour, IInteractable
{
    [Header("Quest Context")]
    [SerializeField] private string _questName;

    [Header("Conversation Selection")]
    [Tooltip("Used when no quest-specific state mapping is available yet.")]
    [SerializeField] private string _defaultConversation = "Guide_Intro";
    [SerializeField] private string _conversationWhenUnassigned = "Guide_Intro";
    [SerializeField] private string _conversationWhenActive = "Guide_QuestActive";
    [SerializeField] private string _conversationWhenReturnToNpc = "Guide_QuestTurnIn";
    [SerializeField] private string _conversationWhenSuccess = "Guide_PostQuest";

    [Header("Debug")]
    [SerializeField] private bool _debugConversationSelection = true;

    public void Interact(GameObject interactor)
    {
        if (interactor == null)
            return;

        string conversation = ResolveConversationTitle();
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
            string stateLabel = string.IsNullOrWhiteSpace(_questName)
                ? "no quest context"
                : PixelCrushersQuestBridge.GetQuestStateString(_questName);
            Debug.Log($"[PixelCrushersConversationInteractable] Starting '{conversation}' for state '{stateLabel}'.", this);
        }
#endif

        PixelCrushersQuestBridge.StartConversation(conversation, interactor.transform, transform);
    }

    private string ResolveConversationTitle()
    {
        if (string.IsNullOrWhiteSpace(_questName))
            return ResolveFallbackConversation();

        string questState = PixelCrushersQuestBridge.GetQuestStateString(_questName);
        switch (questState)
        {
            case QuestLog.ActiveStateString:
                return ResolveOrFallback(_conversationWhenActive);

            case QuestLog.ReturnToNPCStateString:
                return ResolveOrFallback(_conversationWhenReturnToNpc);

            case QuestLog.SuccessStateString:
            case QuestLog.DoneStateString:
                return ResolveOrFallback(_conversationWhenSuccess);

            case QuestLog.UnassignedStateString:
            case QuestLog.GrantableStateString:
            default:
                return ResolveOrFallback(_conversationWhenUnassigned);
        }
    }

    private string ResolveFallbackConversation()
    {
        return string.IsNullOrWhiteSpace(_defaultConversation) ? string.Empty : _defaultConversation;
    }

    private string ResolveOrFallback(string preferredConversation)
    {
        if (!string.IsNullOrWhiteSpace(preferredConversation))
            return preferredConversation;

        return ResolveFallbackConversation();
    }
}
