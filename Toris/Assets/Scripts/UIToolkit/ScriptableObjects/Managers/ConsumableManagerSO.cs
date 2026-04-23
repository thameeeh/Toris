using UnityEngine;

namespace OutlandHaven.Inventory
{
    [CreateAssetMenu(menuName = "UI/Scriptable Objects/ConsumableManagerSO")]
    public class ConsumableManagerSO : ScriptableObject
    {
        [Header("Event Channels")]
        [SerializeField] private UIInventoryEventsSO _uiInventoryEvents;

        // Note: You will likely need a reference to the Player here to apply the actual health/buff.
        // According to your UI docs, you use "Runtime Anchors" for global systems.
        // --> [SerializeField] private PlayerStatsAnchorSO _playerStatsAnchor; 

        private void OnEnable()
        {
            if (_uiInventoryEvents != null)
            {
                // Bind to the exact event fired by PlayerPotionView right-clicks
                _uiInventoryEvents.OnRequestSelectForProcessing += HandleConsumeRequest;
            }
        }

        private void OnDisable()
        {
            if (_uiInventoryEvents != null)
            {
                _uiInventoryEvents.OnRequestSelectForProcessing -= HandleConsumeRequest;
            }
        }

        private void HandleConsumeRequest(InventorySlot slot, string proxyID)
        {
            // 1. Validation: Ensure the slot and item exist
            if (slot == null || slot.IsEmpty || slot.HeldItem?.BaseItem == null)
                return;

            // 2. Type Checking: Ensure it is actually a consumable
            var consumable = slot.HeldItem.BaseItem.GetComponent<ConsumableComponent>();
            if (consumable != null)
            {
                // 3. Execution: Apply the effect to the player
                // consumable.ApplyEffect(_playerAnchor.CurrentPlayer);
                Debug.Log($"[ConsumableManager] Consumed 1x {slot.HeldItem.BaseItem.ItemName}");

                // 4. State Mutation: Decrease the stack
                slot.Count--;

                // If the stack reaches zero, clear the item data from the slot entirely
                if (slot.Count <= 0)
                {
                    slot.Clear();
                }

                // 5. UI Synchronization: Tell the "dumb" UI to redraw just this one slot
                _uiInventoryEvents.OnSpecificSlotsUpdated?.Invoke(slot, null);
            }
            else
            {
                Debug.LogWarning($"[ConsumableManager] Item {slot.HeldItem.BaseItem.ItemName} is not a consumable.");
            }
        }
    }
}