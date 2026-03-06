using System;

namespace OutlandHaven.UIToolkit
{
    [Serializable]
    public class InventorySlot
    {
        public InventoryItemSO Item;
        public int Count;

        public bool IsEmpty => Item == null;

        public void Clear()
        {
            Item = null;
            Count = 0;
        }

        // Helper to add items
        public void SetItem(InventoryItemSO newItem, int amount)
        {
            Item = newItem;
            Count = amount;
        }

        public void IncreaseCount(int amount)
        {
            Count += amount;
        }
    }
}