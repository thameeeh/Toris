using System.Collections.Generic;
using UnityEngine;

namespace OutlandHaven.UIToolkit
{
    [CreateAssetMenu(menuName = "UI/Inventory/Container")]
    public class InventoryContainerSO : ScriptableObject
    {
        public int SlotCount = 20;
        public List<InventorySlot> Slots = new List<InventorySlot>();

        public ScreenType AssociatedView = ScreenType.None;

        [SerializeField] private UIInventoryEventsSO _uiInventoryEvents;
        // Initialize list in editor or runtime
        private void OnEnable()
        {
            // Simple check to ensure list exists
            if (Slots == null) Slots = new List<InventorySlot>();

            // Resize if needed (naive implementation)
            while (Slots.Count < SlotCount)
            {
                Slots.Add(new InventorySlot());
            }

            if(AssociatedView == ScreenType.None)
            {
                Debug.Log("Container is set as None, Change [ScreenType]");
            }
        }

        public bool AddItem (InventoryItemSO item, int quantity)
        {
            // 1. Check for existing stacks first
            foreach (var slot in Slots)
            {
                // If slot has the SAME item and has space
                if (!slot.IsEmpty && slot.Item == item && slot.Count < item.MaxStackSize)
                {
                    int remainingSpace = item.MaxStackSize - slot.Count;
                    int amountToAdd = Mathf.Min(remainingSpace, quantity);

                    slot.IncreaseCount(amountToAdd);
                    quantity -= amountToAdd;

                    // If we added everything, we are done
                    if (quantity <= 0)
                    {
                        _uiInventoryEvents?.OnInventoryUpdated?.Invoke();
                        return true;
                    }
                }
            }

            // 2. If we still have quantity left, find empty slots
            if (quantity > 0)
            {
                foreach (var slot in Slots)
                {
                    if (slot.IsEmpty)
                    {
                        slot.SetItem(item, quantity);
                        _uiInventoryEvents?.OnInventoryUpdated?.Invoke();
                        return true;
                    }
                }
            }

            return false; // Could not add all items (Inventory Full)
        }
    }
}