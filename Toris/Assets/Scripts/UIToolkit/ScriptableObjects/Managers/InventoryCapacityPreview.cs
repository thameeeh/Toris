using System.Collections.Generic;
using OutlandHaven.Inventory;
using UnityEngine;

namespace OutlandHaven.UIToolkit
{
    // Read-only capacity helper for transaction preflights. It mirrors InventoryManager
    // placement rules without mutating live slots, keeping managers focused on decisions.
    internal static class InventoryCapacityPreview
    {
        public static bool CanAddRewardsAfterConsumingSlot(
            InventoryManager inventory,
            InventorySlot consumedSlot,
            IReadOnlyList<CraftingMaterialRequirement> rewards)
        {
            if (inventory == null || inventory.LiveSlots == null || consumedSlot == null)
                return false;

            if (rewards == null)
                return true;

            List<SimulatedSlot> slots = BuildSlotsAfterConsume(inventory, consumedSlot);
            for (int i = 0; i < rewards.Count; i++)
            {
                CraftingMaterialRequirement reward = rewards[i];
                if (reward.Material == null || reward.Quantity <= 0)
                    return false;

                if (!TryPlace(slots, reward.Material, reward.Quantity))
                    return false;
            }

            return true;
        }

        private static List<SimulatedSlot> BuildSlotsAfterConsume(
            InventoryManager inventory,
            InventorySlot consumedSlot)
        {
            // Work against a lightweight snapshot where the selected item is already
            // consumed; reward placement never touches the real inventory here.
            List<SimulatedSlot> slots = new List<SimulatedSlot>(inventory.LiveSlots.Count);
            foreach (InventorySlot liveSlot in inventory.LiveSlots)
            {
                if (liveSlot == null || liveSlot.IsEmpty || liveSlot.HeldItem == null || liveSlot.Count <= 0)
                {
                    slots.Add(new SimulatedSlot(null, 0));
                    continue;
                }

                int count = ReferenceEquals(liveSlot, consumedSlot) ? liveSlot.Count - 1 : liveSlot.Count;
                slots.Add(count > 0
                    ? new SimulatedSlot(liveSlot.HeldItem, count)
                    : new SimulatedSlot(null, 0));
            }

            return slots;
        }

        private static bool TryPlace(
            List<SimulatedSlot> slots,
            InventoryItemSO item,
            int quantity)
        {
            // Match InventoryManager's placement order: fill compatible stacks first,
            // then use empty slots for any remaining quantity.
            ItemInstance itemInstance = new ItemInstance(item);
            bool isStackable = item.MaxStackSize > 1;

            if (isStackable)
            {
                for (int i = 0; i < slots.Count && quantity > 0; i++)
                {
                    SimulatedSlot slot = slots[i];
                    if (slot.IsEmpty || !slot.Item.IsStackableWith(itemInstance))
                        continue;

                    int added = Mathf.Min(item.MaxStackSize - slot.Count, quantity);
                    slot.Count += added;
                    quantity -= added;
                }
            }

            for (int i = 0; i < slots.Count && quantity > 0; i++)
            {
                SimulatedSlot slot = slots[i];
                if (!slot.IsEmpty)
                    continue;

                int added = isStackable ? Mathf.Min(item.MaxStackSize, quantity) : 1;
                slot.Item = itemInstance;
                slot.Count = added;
                quantity -= added;
            }

            return quantity <= 0;
        }

        private sealed class SimulatedSlot
        {
            public SimulatedSlot(ItemInstance item, int count)
            {
                Item = item;
                Count = count;
            }

            public ItemInstance Item { get; set; }
            public int Count { get; set; }
            public bool IsEmpty => Item == null || Item.BaseItem == null || Count <= 0;
        }
    }
}
