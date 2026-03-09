using System;

namespace OutlandHaven.UIToolkit
{
    [Serializable]
    public class InventorySlot
    {
        public ItemInstance HeldItem;
        public int Count;

        public bool IsEmpty => HeldItem == null;

        public void Clear()
        {
            HeldItem = null;
            Count = 0;
        }

        // Helper to add items
        public void SetItem(ItemInstance newItem, int amount)
        {
            HeldItem = newItem;
            Count = amount;
        }

        public void IncreaseCount(int amount)
        {
            Count += amount;
        }

        public void DecreaseCount(int amount)
        {
            Count -= amount;
            if (Count <= 0)
            {
                Clear();
            }
        }
    }
}