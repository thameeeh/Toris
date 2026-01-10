using System;

namespace OutlandHaven.UIToolkit
{
    [Serializable]
    public class InventorySlot
    {
        public InventoryItemSO Item;
        public int Quantity;

        public bool IsEmpty => Item == null;

        public void Clear()
        {
            Item = null;
            Quantity = 0;
        }

        // Helper to add items
        public void SetItem(InventoryItemSO newItem, int amount)
        {
            Item = newItem;
            Quantity = amount;
        }
    }
}