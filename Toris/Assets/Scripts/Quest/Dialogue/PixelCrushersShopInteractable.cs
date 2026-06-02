using OutlandHaven.Inventory;
using OutlandHaven.UIToolkit;
using PixelCrushers.DialogueSystem;
using UnityEngine;

/// <summary>
/// Reusable interactable component for any NPC with shop, upgrade, or specialized services.
/// Checks if the NPC currently has quest interactions:
/// - If true: Plays a conversation presenting choice nodes to access the quest or open the shop.
/// - If false: Bypasses dialogue entirely and immediately opens the specified screen.
/// </summary>
[DisallowMultipleComponent]
public class PixelCrushersShopInteractable : MonoBehaviour, IInteractable
{
    [Header("UI Events")]
    [Tooltip("The UI Events ScriptableObject channel used to broadcast screen open requests.")]
    [SerializeField] private UIEventsSO _uiEvents;

    [Header("Shop Configuration")]
    [Tooltip("The type of UI screen to open for this merchant/NPC.")]
    [SerializeField] private ScreenType _screenType = ScreenType.Smith;

    [Tooltip("The InventoryManager representing this NPC's shop inventory. If null, will search components or local child objects automatically.")]
    [SerializeField] private InventoryManager _shopInventory;

    [Header("Quest / Dialogue Routing")]
    [Tooltip("How the script checks if the NPC currently has a quest to talk about.")]
    [SerializeField] private QuestCheckMethod _checkMethod = QuestCheckMethod.LuaVariable;

    [Tooltip("The name of the boolean Dialogue System variable (e.g., 'isSmithHaveQuest') to check if using LuaVariable method.")]
    [SerializeField] private string _questLuaVariable = "isSmithHaveQuest";

    [Tooltip("The exact Quest Name in the database to check if using QuestState method.")]
    [QuestPopup] [SerializeField] private string _questName = string.Empty;

    [Tooltip("The conversation that starts if the NPC currently has a quest, presenting choice nodes to access the quest or open the shop.")]
    [ConversationPopup] [SerializeField] private string _questSelectionConversation = string.Empty;

    public enum QuestCheckMethod
    {
        None,
        LuaVariable,
        QuestState,
        Both
    }

    private void Awake()
    {
        // Resolve shop inventory component locally if not manually configured
        if (_shopInventory == null)
        {
            _shopInventory = GetComponent<InventoryManager>() ?? GetComponentInChildren<InventoryManager>();
        }
    }

    private void OnValidate()
    {
        if (_uiEvents == null)
        {
            Debug.LogError($"[PixelCrushersShopInteractable] Missing UIEventsSO reference on '{name}'!", this);
        }
    }

    public void Interact(GameObject interactor)
    {
        if (interactor == null) return;

        bool hasQuest = EvaluateQuestAvailability();

        if (hasQuest && !string.IsNullOrWhiteSpace(_questSelectionConversation))
        {
            // Play conversation asking to select Quest vs Shop services
            PixelCrushersQuestBridge.StartConversation(_questSelectionConversation, interactor.transform, transform);
        }
        else
        {
            // Open the shop/service screen directly
            OpenShopScreen();
        }
    }

    private bool EvaluateQuestAvailability()
    {
        if (!DialogueManager.hasInstance)
        {
            Debug.LogWarning($"[PixelCrushersShopInteractable] No active Dialogue Manager found on '{name}'. Bypassing quest check.", this);
            return false;
        }

        switch (_checkMethod)
        {
            case QuestCheckMethod.LuaVariable:
                return !string.IsNullOrWhiteSpace(_questLuaVariable) && DialogueLua.GetVariable(_questLuaVariable).asBool;

            case QuestCheckMethod.QuestState:
                if (string.IsNullOrWhiteSpace(_questName)) return false;
                string state = PixelCrushersQuestBridge.GetQuestStateString(_questName);
                // NPC has quest active if quest is unassigned (available to take), active (in progress), or ready to turn in (ReturnToNPC)
                return state == QuestLog.UnassignedStateString || state == QuestLog.ReturnToNPCStateString || state == QuestLog.ActiveStateString;

            case QuestCheckMethod.Both:
                bool luaCheck = !string.IsNullOrWhiteSpace(_questLuaVariable) && DialogueLua.GetVariable(_questLuaVariable).asBool;
                bool stateCheck = false;
                if (!string.IsNullOrWhiteSpace(_questName))
                {
                    string s = PixelCrushersQuestBridge.GetQuestStateString(_questName);
                    stateCheck = s == QuestLog.UnassignedStateString || s == QuestLog.ReturnToNPCStateString || s == QuestLog.ActiveStateString;
                }
                return luaCheck || stateCheck;

            case QuestCheckMethod.None:
            default:
                return false;
        }
    }

    /// <summary>
    /// Opens this NPC's shop/service screen.
    /// Can be called from external scripts or Dialogue System sequences via SendMessage.
    /// </summary>
    public void OpenShop()
    {
        OpenShopScreen();
    }

    private void OpenShopScreen()
    {
        if (_uiEvents == null)
        {
            Debug.LogError($"[PixelCrushersShopInteractable] Cannot open shop screen on '{name}' because UIEventsSO is not assigned!", this);
            return;
        }

        _uiEvents.OnRequestOpen?.Invoke(_screenType, _shopInventory);
    }
}
