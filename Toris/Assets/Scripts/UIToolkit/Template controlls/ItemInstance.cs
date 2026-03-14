using System;
using UnityEngine;

namespace OutlandHaven.UIToolkit
{
    /// <summary>
    /// Wrapper class holding the runtime state of an item.
    /// </summary>
    [Serializable]
    public class ItemInstance
    {
        public InventoryItemSO BaseItem;
        public int CurrentLevel = 1;
        public float Durability = 100f; // Example skeleton stat

        // You can add more instance-specific stats here later

        public ItemInstance()
        {
            CurrentLevel = 1;
            Durability = 100f;
        }

        public ItemInstance(InventoryItemSO baseItem)
        {
            BaseItem = baseItem;
            CurrentLevel = 1;
            Durability = 100f;
        }

        public ItemInstance(InventoryItemSO baseItem, int startLevel)
        {
            BaseItem = baseItem;
            CurrentLevel = startLevel;
            Durability = 100f;
        }

        /// <summary>
        /// Checks if this ItemInstance is effectively identical to another
        /// (meaning they can stack together).
        /// </summary>
        public bool IsStackableWith(ItemInstance other)
        {
            if (other == null) return false;

            // They can only stack if they are the exact same base item
            // and have the exact same level/stats.
            return BaseItem == other.BaseItem && CurrentLevel == other.CurrentLevel;
        }
    }
}
