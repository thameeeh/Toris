using OutlandHaven.Inventory;
using OutlandHaven.UIToolkit;
using PixelCrushers.DialogueSystem;
using UnityEngine;

/// <summary>
/// Specialized interactable component for the Smith NPC.
/// Checks if the Dialogue System variable 'isSmithHaveQuest' is true:
/// - If true: Launches the quest-or-shop dialogue routing conversation tree.
/// - If false: Bypasses dialogue entirely and immediately opens the Smith Screen panel.
/// </summary>
[DisallowMultipleComponent]
public class PixelCrushersSmithInteractable : MonoBehaviour, IInteractable
{
    [Header("UI Events")]
    [Tooltip("The UI Events ScriptableObject channel used to broadcast screen open requests.")]
    [SerializeField] private UIEventsSO _uiEvents;

    [Header("Dialogue / Quest Integration")]
    [Tooltip("The boolean Dialogue System variable in the database that tracks if the Smith currently has quest interactions.")]
    [SerializeField] private string _questVariable = "isSmithHaveQuest";

    [Tooltip("The conversation that starts if the Smith has a quest, presenting choice nodes to access the quest or open the smith panel.")]
    [ConversationPopup] [SerializeField] private string _questSelectionConversation = "Smith_Quest_Or_Shop";

    [Header("Shop / Inventory Configuration")]
    [Tooltip("The InventoryManager representing the Smith's shop inventory. If null, will search components or local child objects automatically.")]
    [SerializeField] private InventoryManager _shopInventory;

    private void Awake()
    {
        // Resolve shop inventory component locally if not manually configured
        if (_shopInventory == null)
        {
            _shopInventory = GetComponent<InventoryManager>();
            if (_shopInventory == null)
            {
                _shopInventory = GetComponentInChildren<InventoryManager>();
            }
        }
    }

    private void OnValidate()
    {
        if (_uiEvents == null)
        {
            Debug.LogError($"[PixelCrushersSmithInteractable] Missing UIEventsSO reference on GameObject '{name}'!", this);
        }
    }

    public void Interact(GameObject interactor)
    {
        if (interactor == null)
            return;

        bool hasQuest = false;

        // Check the dialogue database variable
        if (DialogueManager.hasInstance)
        {
            hasQuest = DialogueLua.GetVariable(_questVariable).asBool;
        }
        else
        {
            Debug.LogWarning("[PixelCrushersSmithInteractable] No active Dialogue Manager instance found. Defaulting to Smith Screen direct access.", this);
        }

        if (hasQuest)
        {
            if (!string.IsNullOrWhiteSpace(_questSelectionConversation))
            {
                // Play conversation asking to select Quest vs Smith services
                PixelCrushersQuestBridge.StartConversation(_questSelectionConversation, interactor.transform, transform);
            }
            else
            {
                Debug.LogWarning($"[PixelCrushersSmithInteractable] '{_questVariable}' is true, but no selection conversation is assigned! Opening Smith screen directly as fallback.", this);
                OpenSmithScreen();
            }
        }
        else
        {
            // Open the Smith screen directly
            OpenSmithScreen();
        }
    }

    private void OpenSmithScreen()
    {
        if (_uiEvents == null)
        {
            Debug.LogError("[PixelCrushersSmithInteractable] Cannot open Smith Screen because UIEventsSO is not assigned!", this);
            return;
        }

        // Trigger opening the Smith screen.
        // If _shopInventory is null, PlayerInventorySceneResolver will dynamically resolve it.
        _uiEvents.OnRequestOpen?.Invoke(ScreenType.Smith, _shopInventory);
    }
}
