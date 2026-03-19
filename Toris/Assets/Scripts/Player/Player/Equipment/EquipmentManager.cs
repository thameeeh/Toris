using System;
using System.Collections.Generic;
using UnityEngine;
using OutlandHaven.Inventory;

namespace OutlandHaven.Player.Equipment
{
    /// <summary>
    /// Tracks what ItemInstance is in which EquipmentSlot for the player.
    /// This acts purely as a data layer and remains ignorant of player stats.
    /// </summary>
    public class EquipmentManager : MonoBehaviour
    {
        // Internal dictionary mapping equipment slots to the currently equipped ItemInstance.
        private readonly Dictionary<EquipmentSlot, ItemInstance> _equippedItems = new();

        /// <summary>
        /// Fired whenever an item is equipped or unequipped.
        /// Payload: (slot, oldItem, newItem)
        /// </summary>
        public event Action<EquipmentSlot, ItemInstance, ItemInstance> OnEquipmentChanged;

        /// <summary>
        /// Retrieves the currently equipped item in the specified slot, if any.
        /// </summary>
        public ItemInstance GetEquippedItem(EquipmentSlot slot)
        {
            return _equippedItems.TryGetValue(slot, out ItemInstance item) ? item : null;
        }

        /// <summary>
        /// Attempts to equip the provided item to the specified slot.
        /// Will trigger OnEquipmentChanged with the old item (if any) and the new item.
        /// Returns the previously equipped item to be returned to the inventory.
        /// </summary>
        public ItemInstance EquipItem(ItemInstance newItem, EquipmentSlot targetSlot)
        {
            if (newItem == null)
            {
                return UnequipItem(targetSlot);
            }

            // Ensure the item has an equipable component and it matches the slot
            EquipableComponent equipable = newItem.BaseItem.GetComponent<EquipableComponent>();
            if (equipable == null)
            {
                Debug.LogWarning($"[EquipmentManager] Attempted to equip an item ({newItem.BaseItem.ItemName}) without an EquipableComponent.");
                return null;
            }

            if (equipable.TargetSlot != targetSlot)
            {
                Debug.LogWarning($"[EquipmentManager] Attempted to equip {newItem.BaseItem.ItemName} to {targetSlot}, but it belongs in {equipable.TargetSlot}.");
                return null;
            }

            // Perform the swap
            ItemInstance oldItem = GetEquippedItem(targetSlot);
            _equippedItems[targetSlot] = newItem;

            OnEquipmentChanged?.Invoke(targetSlot, oldItem, newItem);

            return oldItem;
        }

        /// <summary>
        /// Unequips whatever is in the target slot and returns it.
        /// </summary>
        public ItemInstance UnequipItem(EquipmentSlot targetSlot)
        {
            ItemInstance oldItem = GetEquippedItem(targetSlot);
            if (oldItem != null)
            {
                _equippedItems.Remove(targetSlot);
                OnEquipmentChanged?.Invoke(targetSlot, oldItem, null);
            }
            return oldItem;
        }

        /// <summary>
        /// Convenience method to clear all equipment.
        /// </summary>
        public void ClearAllEquipment()
        {
            // Create a copy of the keys to avoid collection modified exceptions
            var slots = new List<EquipmentSlot>(_equippedItems.Keys);
            foreach (var slot in slots)
            {
                UnequipItem(slot);
            }
        }
    }
}