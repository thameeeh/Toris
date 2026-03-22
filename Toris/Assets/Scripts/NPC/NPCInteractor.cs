using UnityEngine;
using OutlandHaven.Inventory; // Needed for InventoryManager
using OutlandHaven.UIToolkit; // Needed for UIEventsSO and ScreenType

public class NPCInteraction : MonoBehaviour
{
    [SerializeField] private PlayerInputReaderSO _inputReader; // The same SO in your InputManager

    [Header("Shop Settings")]
    [Tooltip("The event channel used to open UIs")]
    [SerializeField] private UIEventsSO _uiEvents;

    [Tooltip("The inventory manager attached to this NPC")]
    [SerializeField] private InventoryManager _myInventory;

    [Tooltip("Which UI screen should this NPC open?")]
    [SerializeField] private ScreenType _shopType = ScreenType.Smith;

    private bool _isPlayerInRange = false;

    private void Awake()
    {
        // Safety check: if you forgot to drag the inventory in the inspector, try to find it on this GameObject automatically
        if (_myInventory == null)
        {
            _myInventory = GetComponent<InventoryManager>();
        }
    }

    private void OnEnable()
    {
        // Subscribe to the event when the NPC is active
        _inputReader.OnInteractPressed += HandleInteraction;
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent memory leaks/errors
        _inputReader.OnInteractPressed -= HandleInteraction;
    }

    private void HandleInteraction()
    {
        // Only do something if the player is actually standing here
        if (_isPlayerInRange)
        {
            PerformDialogue();
        }
    }

    private void PerformDialogue()
    {
        Debug.Log("The NPC says: Welcome to my shop!");

        // Open the Smith UI and pass this NPC's specific inventory as the payload
        if (_uiEvents != null && _myInventory != null)
        {
            _uiEvents.OnRequestOpen?.Invoke(_shopType, _myInventory);
        }
        else
        {
            Debug.LogError("NPC is missing UIEventsSO or InventoryManager reference!");
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Optional: you should ideally use TryGetComponent instead of CompareTag for better performance in physics events
        if (collision.CompareTag("Player")) _isPlayerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player")) _isPlayerInRange = false;
    }
}