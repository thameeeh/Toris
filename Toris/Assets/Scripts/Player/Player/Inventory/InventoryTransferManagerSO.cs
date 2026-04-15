using System;
using UnityEngine;
using OutlandHaven.UIToolkit;

namespace OutlandHaven.Inventory
{
    [CreateAssetMenu(menuName = "UI/Scriptable Objects/Managers/InventoryTransferManagerSO")]
    public class InventoryTransferManagerSO : ScriptableObject
    {
        [SerializeField] private UIInventoryEventsSO _uiInventoryEvents;

        private void OnEnable()
        {
            if (_uiInventoryEvents != null)
            {
                _uiInventoryEvents.OnRequestMoveItem += HandleMoveItemRequest;
            }
        }

        private void OnDisable()
        {
            if (_uiInventoryEvents != null)
            {
                _uiInventoryEvents.OnRequestMoveItem -= HandleMoveItemRequest;
            }
        }

        private void HandleMoveItemRequest(InventoryManager sourceContainer, InventorySlot sourceSlot, InventoryManager targetContainer, InventorySlot targetSlot, int amountToMove)
        {
            if (sourceContainer == null || sourceSlot == null || targetContainer == null || targetSlot == null) return;
            if (sourceSlot.IsEmpty) return; // Cannot move an empty slot
            if (sourceSlot == targetSlot) return; // Cannot drop on the same slot
            if (amountToMove <= 0) return; // Cannot move 0 or negative items

            // --- NEW: SMART VALIDATION ---
            // Ask the slot if it will accept the item. The Manager doesn't need to know WHY.
            if (!targetSlot.CanAccept(sourceSlot.HeldItem))
            {
                Debug.LogWarning("Drag Failed: Target slot refuses this item type.");
                return;
            }

            // If it's a swap, ask the source slot if it will accept the target's item coming back
            if (!targetSlot.IsEmpty && !sourceSlot.CanAccept(targetSlot.HeldItem))
            {
                Debug.LogWarning("Drag Swap Failed: Source slot refuses the swapped item.");
                return;
            }
            // ----------------------------

            // Ensure we don't try to move more than we actually have
            int actualAmount = Mathf.Min(amountToMove, sourceSlot.Count);

            // 1. Is the target slot empty? (Full Move or Split)
            if (targetSlot.IsEmpty)
            {
                targetSlot.SetItem(sourceSlot.HeldItem, actualAmount);
                sourceSlot.DecreaseCount(actualAmount);
            }
            // 2. Is the target slot holding the same stackable item type? (Stack)
            else if (targetSlot.HeldItem.IsStackableWith(sourceSlot.HeldItem))
            {
                int maxStackSize = targetSlot.HeldItem.BaseItem.MaxStackSize;
                int spaceInTarget = maxStackSize - targetSlot.Count;

                if (spaceInTarget > 0)
                {
                    int amountWeCanMove = Mathf.Min(spaceInTarget, actualAmount);

                    targetSlot.IncreaseCount(amountWeCanMove);
                    sourceSlot.DecreaseCount(amountWeCanMove);
                }
            }
            // 3. Is the target slot holding a different item? (Swap)
            else
            {
                // You cannot perform a swap if you are only moving a partial stack.
                if (actualAmount != sourceSlot.Count)
                {
                    Debug.LogWarning("Drag Swap Failed: Cannot swap a partial stack with a different item.");
                    return;
                }

                // Perform a direct swap of the item instances and counts
                ItemInstance tempItem = targetSlot.HeldItem;
                int tempCount = targetSlot.Count;

                targetSlot.SetItem(sourceSlot.HeldItem, sourceSlot.Count);
                sourceSlot.SetItem(tempItem, tempCount);
            }

            // Cleanup: If the source slot is now empty after a partial move or stack, clear it.
            if (sourceSlot.Count <= 0)
            {
                sourceSlot.Clear();
            }

            // Fire events to notify listeners that inventories have changed
            _uiInventoryEvents.OnSpecificSlotsUpdated?.Invoke(sourceSlot, targetSlot);
        }
    }
}
