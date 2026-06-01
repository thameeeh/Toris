using UnityEngine;
using System.Collections.Generic;
using PixelCrushers.DialogueSystem;

/// <summary>
/// Overhead quest indicator controller for Outland Haven NPCs.
/// Supports tracking multiple quests on a single NPC, with built-in priority resolution.
/// Tracks only Unassigned (Available) and ReturnToNPC (Ready to Turn-in) states.
/// </summary>
public class QuestIndicatorController : MonoBehaviour
{
    [Header("Quest Settings")]
    [Tooltip("All quests this NPC handles. The controller automatically prioritizes ready turn-ins over new quests.")]
    [QuestPopup] [SerializeField] private List<string> _quests = new List<string>();
    
    [Header("Visual Indicators")]
    [Tooltip("Active when at least one quest is Unassigned (Available). Usually an exclamation mark !")]
    [SerializeField] private GameObject _availableIndicator;
    
    [Tooltip("Active when at least one quest is ReturnToNPC (Ready to turn in). Usually a glowing question mark ?")]
    [SerializeField] private GameObject _readyToTurnInIndicator;

    private bool _isSubscribed = false;

    private void Start()
    {
        SubscribeToEvents();
        UpdateIndicatorState();
    }

    private void OnEnable()
    {
        SubscribeToEvents();
        UpdateIndicatorState();
    }

    private void OnDisable()
    {
        UnsubscribeFromEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void SubscribeToEvents()
    {
        if (_isSubscribed || !DialogueManager.hasInstance) return;
        
        // Dynamically add DialogueSystemEvents to the Dialogue Manager if it is missing
        // to guarantee that Unity BroadcastMessage events are caught and propagated.
        var dsEvents = DialogueManager.instance.GetComponent<DialogueSystemEvents>();
        if (dsEvents == null)
        {
            dsEvents = DialogueManager.instance.gameObject.AddComponent<DialogueSystemEvents>();
        }

        if (dsEvents != null)
        {
            dsEvents.questEvents.onQuestStateChange.AddListener(OnQuestStateChanged);
            _isSubscribed = true;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (!_isSubscribed || !DialogueManager.hasInstance) return;
        
        var dsEvents = DialogueManager.instance.GetComponent<DialogueSystemEvents>();
        if (dsEvents != null)
        {
            dsEvents.questEvents.onQuestStateChange.RemoveListener(OnQuestStateChanged);
        }
        _isSubscribed = false;
    }

    private void OnQuestStateChanged(string questName)
    {
        if (_quests != null && _quests.Contains(questName))
        {
            UpdateIndicatorState();
        }
    }

    public void UpdateIndicatorState()
    {
        if (_quests == null || _quests.Count == 0)
        {
            SetAllIndicatorsActive(false);
            return;
        }

        bool showAvailable = false;
        bool showReadyToTurnIn = false;

        // 1. Evaluate states for all quests in our list
        foreach (var qName in _quests)
        {
            if (string.IsNullOrWhiteSpace(qName) || string.Equals(qName, "None", System.StringComparison.OrdinalIgnoreCase))
                continue;

            if (!DialogueLua.DoesTableElementExist("Quest", qName))
            {
#if UNITY_EDITOR
                if (DialogueManager.hasInstance)
                {
                    Debug.LogWarning($"[QuestIndicatorController] Quest '{qName}' on '{gameObject.name}' does not exist in the Dialogue Database! Please check your spelling.", this);
                }
#endif
                continue;
            }

            QuestState state = QuestLog.GetQuestState(qName);

            if (state == QuestState.ReturnToNPC)
            {
                showReadyToTurnIn = true;
            }
            else if (state == QuestState.Unassigned)
            {
                showAvailable = true;
            }
        }

        // 2. Resolve Priorities:
        // Turn-in (Ready) has highest priority, then Available
        if (showReadyToTurnIn)
        {
            showAvailable = false;
        }

        // 3. Set visual states
        if (_availableIndicator != null) _availableIndicator.SetActive(showAvailable);
        if (_readyToTurnInIndicator != null) _readyToTurnInIndicator.SetActive(showReadyToTurnIn);
        
        // Log diagnostic information to the console so developers can easily track indicator states
        if (DialogueDebug.logInfo && _quests.Count > 0)
        {
            Debug.Log($"[QuestIndicatorController] Updated indicators on '{gameObject.name}': Available={showAvailable}, ReadyToTurnIn={showReadyToTurnIn}", this);
        }
    }

    private void SetAllIndicatorsActive(bool active)
    {
        if (_availableIndicator != null) _availableIndicator.SetActive(active);
        if (_readyToTurnInIndicator != null) _readyToTurnInIndicator.SetActive(active);
    }
}
