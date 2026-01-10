using System.Collections.Generic;
using UnityEngine;

namespace OutlandHaven.UIToolkit
{
    [CreateAssetMenu(menuName = "UI/Inventory/Container")]
    public class InventoryContainerSO : ScriptableObject
    {
        public int SlotCount = 20;
        public List<InventorySlot> Slots = new List<InventorySlot>();

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
        }

        //add methods like AddItem, RemoveItem here later
    }
}