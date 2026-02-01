using UnityEngine;

namespace OutlandHaven.UIToolkit
{
    public class WorldContainer : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private InventoryContainerSO _containerData; // Drag 'Container_VillageChest' here
        [SerializeField] private UIEventsSO _UIEvents;

        [Header("Interaction")]
        [SerializeField] private KeyCode _interactKey = KeyCode.F;
        private bool _playerInRange = false;

        private void OnValidate()
        {
            if (_UIEvents == null)
            {
                Debug.LogError($"<color=red>Paperdau</color> {name} is missing, put SO in the inspector!", this);
            }
        }

        private void Update()
        {
            // Only allow opening if player is close AND presses F
            if (_playerInRange && Input.GetKeyDown(_interactKey))
            {
                Debug.Log("Interact key pressed.");
                OpenContainer();
            }
        }

        private void OpenContainer()
        {
            Debug.Log($"Opening Container: {_containerData.name}");

            // KEY MOMENT: Fire the event with the Chest Data as the Payload!
            _UIEvents.OnRequestOpen?.Invoke(_containerData.AssociatedView, _containerData);
        }

        // Detect Player entering the trigger zone
        private void OnTriggerEnter2D(Collider2D other)
        {
            // Make sure your Player object has the tag "Player"
            if (other.CompareTag("Player"))
            {
                _playerInRange = true;
                Debug.Log("Player near chest. Press 'F' to open.");
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                _playerInRange = false;
                // Optional: Auto-close UI when walking away
                _UIEvents.OnRequestClose?.Invoke(ScreenType.Inventory);
            }
        }
    }
}