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

        private void HandleMoveItemRequest(InventoryManager sourceContainer, InventorySlot sourceSlot, InventoryManager targetContainer, InventorySlot targetSlot)
        {
            if (sourceContainer == null || sourceSlot == null || targetContainer == null || targetSlot == null) return;
            if (sourceSlot.IsEmpty) return; // Cannot move an empty slot

            // --- NEW: VALIDATION STEP ---
            if (!IsValidEquipmentMove(sourceSlot, targetContainer, targetSlot))
            {
                Debug.LogWarning("Drag Failed: Item does not match equipment slot requirements.");
                return; // Abort the transfer
            }
            // ----------------------------

            // 1. Is the target slot empty?
            if (targetSlot.IsEmpty)
            {
                // Move item directly
                targetSlot.SetItem(sourceSlot.HeldItem, sourceSlot.Count);
                sourceSlot.Clear();
            }
            // 2. Is the target slot holding the same stackable item type?
            else if (targetSlot.HeldItem.IsStackableWith(sourceSlot.HeldItem))
            {
                int maxStackSize = targetSlot.HeldItem.BaseItem.MaxStackSize;
                int spaceInTarget = maxStackSize - targetSlot.Count;

                if (spaceInTarget > 0)
                {
                    int amountToMove = Mathf.Min(spaceInTarget, sourceSlot.Count);

                    targetSlot.IncreaseCount(amountToMove);
                    sourceSlot.DecreaseCount(amountToMove);
                }
            }
            // 3. Is the target slot holding a different item? (Swap)
            else
            {
                // --- NEW: SWAP VALIDATION ---
                // If we are swapping, we also must ensure the item COMING BACK to the source slot is allowed!
                // (e.g., If we drag a helmet onto a sword in the equipment screen, they can't swap).
                if (!IsValidEquipmentMove(targetSlot, sourceContainer, sourceSlot))
                {
                    Debug.LogWarning("Drag Swap Failed: Target item cannot be moved to the Source container's slot.");
                    return;
                }

                // Perform a direct swap of the item instances and counts
                ItemInstance tempItem = targetSlot.HeldItem;
                int tempCount = targetSlot.Count;

                targetSlot.SetItem(sourceSlot.HeldItem, sourceSlot.Count);
                sourceSlot.SetItem(tempItem, tempCount);
            }

            // Fire events to notify listeners that inventories have changed
            _uiInventoryEvents.OnInventoryUpdated?.Invoke();
        }

        private bool IsValidEquipmentMove(InventorySlot sourceSlot, InventoryManager targetContainer, InventorySlot targetSlot)
        {
            // 1. Check if the target container is an Equipment Inventory.
            // Assuming your Character Sheet / Equipment screen uses ScreenType.CharacterSheet or similar.
            // Adjust this enum check to match whatever AssociatedView your Equipment container uses!
            if (targetContainer.ContainerBlueprint == null || targetContainer.ContainerBlueprint.AssociatedView != ScreenType.CharacterSheet)
            {
                return true; // It's a normal inventory (like a chest or backpack), so it's a valid move.
            }

            // 2. We are moving into equipment. Does the item have an EquipableComponent?
            EquipableComponent equipable = sourceSlot.HeldItem.BaseItem.GetComponent<EquipableComponent>();
            if (equipable == null) return false; // Item cannot be equipped at all.

            // 3. Find the index of the target slot in the container
            int targetIndex = targetContainer.LiveSlots.IndexOf(targetSlot);

            // 4. Compare the item's target slot to your hardcoded PlayerEquipmentController index mapping.
            // Index 0 = Head, 1 = Chest, 2 = Legs, 3 = Arms, 4 = Weapon
            return targetIndex == (int)equipable.TargetSlot;
        }
    }
}