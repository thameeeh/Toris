using UnityEngine;

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
                // Perform a direct swap of the item instances and counts
                ItemInstance tempItem = targetSlot.HeldItem;
                int tempCount = targetSlot.Count;

                targetSlot.SetItem(sourceSlot.HeldItem, sourceSlot.Count);
                sourceSlot.SetItem(tempItem, tempCount);
            }

            // Fire events to notify listeners that inventories have changed
            _uiInventoryEvents.OnInventoryUpdated?.Invoke();
        }
    }
}