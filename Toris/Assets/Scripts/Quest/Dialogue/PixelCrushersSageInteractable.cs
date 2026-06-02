using OutlandHaven.Inventory;
using OutlandHaven.UIToolkit;
using PixelCrushers.DialogueSystem;
using UnityEngine;

/// <summary>
/// Specialized interactable component for the Sage NPC.
/// Checks if the Dialogue System variable 'isSageHaveQuest' is true:
/// - If true: Launches the quest-or-upgrade dialogue routing conversation tree.
/// - If false: Bypasses dialogue entirely and immediately opens the Sage Upgrade Screen.
/// </summary>
[DisallowMultipleComponent]
public class PixelCrushersSageInteractable : MonoBehaviour, IInteractable
{
    [Header("UI Events")]
    [Tooltip("The UI Events ScriptableObject channel used to broadcast screen open requests.")]
    [SerializeField] private UIEventsSO _uiEvents;

    [Header("Dialogue / Quest Integration")]
    [Tooltip("The boolean Dialogue System variable in the database that tracks if the Sage currently has quest interactions.")]
    [SerializeField] private string _questVariable = "isSageHaveQuest";

    [Tooltip("The conversation that starts if the Sage has a quest, presenting choice nodes to access the quest or open the upgrade panel.")]
    [ConversationPopup] [SerializeField] private string _questSelectionConversation = "Sage";

    private void OnValidate()
    {
        if (_uiEvents == null)
        {
            Debug.LogError($"[PixelCrushersSageInteractable] Missing UIEventsSO reference on GameObject '{name}'!", this);
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
            Debug.LogWarning("[PixelCrushersSageInteractable] No active Dialogue Manager instance found. Defaulting to Sage Screen direct access.", this);
        }

        if (hasQuest)
        {
            if (!string.IsNullOrWhiteSpace(_questSelectionConversation))
            {
                // Play conversation asking to select Quest vs Sage services
                PixelCrushersQuestBridge.StartConversation(_questSelectionConversation, interactor.transform, transform);
            }
            else
            {
                Debug.LogWarning($"[PixelCrushersSageInteractable] '{_questVariable}' is true, but no selection conversation is assigned! Opening Sage Upgrade screen directly as fallback.", this);
                OpenSageUpgradeScreen();
            }
        }
        else
        {
            // Open the Sage Upgrade screen directly
            OpenSageUpgradeScreen();
        }
    }

    /// <summary>
    /// Opens the Sage shop/service screen.
    /// Can be called from external scripts or Dialogue System sequences via SendMessage.
    /// </summary>
    public void OpenShop()
    {
        OpenSageUpgradeScreen();
    }

    private void OpenSageUpgradeScreen()
    {
        if (_uiEvents == null)
        {
            Debug.LogError("[PixelCrushersSageInteractable] Cannot open Sage Upgrade Screen because UIEventsSO is not assigned!", this);
            return;
        }

        // Trigger opening the Sage Upgrade screen.
        _uiEvents.OnRequestOpen?.Invoke(ScreenType.SageUpgrade, null);
    }
}
