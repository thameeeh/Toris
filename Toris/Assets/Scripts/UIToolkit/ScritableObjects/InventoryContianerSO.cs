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
            // Simple implementation: add to first empty slot
            foreach (var slot in Slots)
            {
                if (slot.IsEmpty)
                {
                    slot.SetItem(item, quantity);
                    return true;
                }
            }
            return false; // No empty slots
        }
        //add methods like AddItem, RemoveItem here later
    }
}