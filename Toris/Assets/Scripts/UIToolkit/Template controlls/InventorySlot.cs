using System;

namespace OutlandHaven.Inventory
{
    public enum SlotFilterType
    {
        Any = -1,
        Head = EquipmentSlot.Head,
        Chest = EquipmentSlot.Chest,
        Legs = EquipmentSlot.Legs,
        Arms = EquipmentSlot.Arms,
        Weapon = EquipmentSlot.Weapon
    }

    [Serializable]
    public class InventorySlot
    {
        public ItemInstance HeldItem;
        public int Count;
        public SlotFilterType AllowedFilter = SlotFilterType.Any;

        public bool IsEmpty => HeldItem == null || HeldItem.BaseItem == null;

        public InventorySlot(SlotFilterType filter = SlotFilterType.Any)
        {
            HeldItem = new ItemInstance();
            Count = 0;
            AllowedFilter = filter;
        }

        public bool CanAccept(ItemInstance item)
        {
            if (item == null || item.BaseItem == null) return false;
            if (AllowedFilter == SlotFilterType.Any) return true;

            // If it's a restricted slot, check the item's components
            EquipableComponent equipable = item.BaseItem.GetComponent<EquipableComponent>();
            if (equipable == null) return false; // Not an equippable item

            // Compare the slot's filter to the item's intended slot
            return (int)AllowedFilter == (int)equipable.TargetSlot;
        }

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