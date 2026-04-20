using System.Collections.Generic;
using UnityEngine;
using OutlandHaven.UIToolkit;

namespace OutlandHaven.Inventory
{
    public class InventoryManager : MonoBehaviour
    {
        [Tooltip("The rules for this specific inventory.")]
        public InventoryContainerSO ContainerBlueprint;

        [Tooltip("The live slots holding the player's actual items.")]
        public List<InventorySlot> LiveSlots = new List<InventorySlot>();

        [SerializeField] private UIInventoryEventsSO _uiInventoryEvents;

        public GameSessionSO GlobalSession;

        private void Awake()
        {
            // Initialize the live slots based on the SO's rules when the object spawns
            if (ContainerBlueprint != null)
            {
                // Ensure LiveSlots count exactly matches the Blueprint's SlotCount
                while (LiveSlots.Count < ContainerBlueprint.SlotCount)
                {
                    int index = LiveSlots.Count;
                    SlotFilterType filter = (ContainerBlueprint.PredefinedFilters != null && index < ContainerBlueprint.PredefinedFilters.Length)
                        ? ContainerBlueprint.PredefinedFilters[index]
                        : SlotFilterType.Any;
                    LiveSlots.Add(new InventorySlot(filter));
                }
                while (LiveSlots.Count > ContainerBlueprint.SlotCount)
                {
                    LiveSlots.RemoveAt(LiveSlots.Count - 1);
                }
            }

            EnsureSlotItemStates();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Keep the LiveSlots count synchronized with the Blueprint's SlotCount in the Editor
            if (ContainerBlueprint != null)
            {
                if (LiveSlots == null)
                {
                    LiveSlots = new List<InventorySlot>();
                }

                while (LiveSlots.Count < ContainerBlueprint.SlotCount)
                {
                    int index = LiveSlots.Count;
                    SlotFilterType filter = (ContainerBlueprint.PredefinedFilters != null && index < ContainerBlueprint.PredefinedFilters.Length)
                        ? ContainerBlueprint.PredefinedFilters[index]
                        : SlotFilterType.Any;
                    LiveSlots.Add(new InventorySlot(filter));
                }
                while (LiveSlots.Count > ContainerBlueprint.SlotCount)
                {
                    LiveSlots.RemoveAt(LiveSlots.Count - 1);
                }

                foreach (var slot in LiveSlots)
                {
                    if (slot != null)
                    {
                        // If an item is assigned but the count is 0 (or negative), force it to 1
                        if (slot.HeldItem != null && slot.HeldItem.BaseItem != null && slot.Count <= 0)
                        {
                            // Note: Depending on how InventorySlot is written, you might need to adjust this line.
                            // If Count is read-only, use your SetItem method:
                            slot.SetItem(slot.HeldItem, 1);
                        }
                        // Optional cleanup: If the item was deleted from the inspector but the count remained, clear it
                        else if ((slot.HeldItem == null || slot.HeldItem.BaseItem == null) && slot.Count > 0)
                        {
                            slot.Clear();
                        }
                    }
                }

                EnsureSlotItemStates();
                ValidateSceneSlotStacking();
            }
        }
#endif

        private void OnEnable()
        {
            if (GlobalSession != null && ContainerBlueprint != null && ContainerBlueprint.AssociatedView == ScreenType.Inventory)
            {
                GlobalSession.PlayerInventory = this;
            }
        }

        private void OnDisable()
        {
            // Crucial: Prevent memory leaks or dangling references when the scene unloads
            if (GlobalSession != null && ContainerBlueprint != null && ContainerBlueprint.AssociatedView == ScreenType.Inventory)
            {
                if (GlobalSession.PlayerInventory == this)
                {
                    GlobalSession.PlayerInventory = null;
                }
            }
        }

        public bool AddItem(ItemInstance itemInstance, int quantity)
        {
            // 1. Pre-calculate if we have enough space BEFORE modifying anything
            int totalSpaceAvailable = CalculateAvailableSpace(itemInstance);
            if (totalSpaceAvailable < quantity)
            {
                return false; // Safely abort without corrupting data
            }

            // 2. We know it fits, so we can safely add it to existing stacks
            foreach (var slot in LiveSlots)
            {
                if (!slot.IsEmpty && slot.HeldItem.IsStackableWith(itemInstance) && slot.Count < itemInstance.BaseItem.MaxStackSize)
                {
                    int spaceInStack = itemInstance.BaseItem.MaxStackSize - slot.Count;
                    int amountToAdd = Mathf.Min(spaceInStack, quantity);

                    slot.IncreaseCount(amountToAdd);
                    quantity -= amountToAdd;

                    if (quantity <= 0) break;
                }
            }

            // 3. Put any leftovers into empty slots
            if (quantity > 0)
            {
                foreach (var slot in LiveSlots)
                {
                    if (slot.IsEmpty)
                    {
                        int spaceInStack = itemInstance.BaseItem.MaxStackSize;
                        int amountToAdd = Mathf.Min(spaceInStack, quantity);

                        // CRITICAL FIX: Clone the item so the new slot gets its own memory reference
                        ItemInstance newStack = itemInstance.Clone();

                        slot.SetItem(newStack, amountToAdd);
                        quantity -= amountToAdd;

                        if (quantity <= 0) break;
                    }
                }
            }

            _uiInventoryEvents?.OnInventoryUpdated?.Invoke();
            return true;
        }

        public bool RemoveItem(ItemInstance itemInstance, int quantity)
        {
            // 1. First pass: verify we have enough total items BEFORE removing any
            int totalAvailable = 0;
            foreach (var slot in LiveSlots)
            {
                if (!slot.IsEmpty && slot.HeldItem.IsStackableWith(itemInstance))
                {
                    totalAvailable += slot.Count;
                }
            }

            if (totalAvailable < quantity)
            {
                return false; // Not enough items, abort transaction
            }

            // 2. Second pass: actually execute the removal
            int remainingToRemove = quantity;

            foreach (var slot in LiveSlots)
            {
                if (!slot.IsEmpty && slot.HeldItem.IsStackableWith(itemInstance))
                {
                    if (slot.Count >= remainingToRemove)
                    {
                        slot.DecreaseCount(remainingToRemove);
                        _uiInventoryEvents?.OnInventoryUpdated?.Invoke();
                        return true;
                    }
                    else
                    {
                        remainingToRemove -= slot.Count;
                        slot.Clear();
                    }
                }
            }

            return false; // Failsafe
        }

        // Helper method to safely calculate space
        private int CalculateAvailableSpace(ItemInstance itemInstance)
        {
            int space = 0;
            foreach (var slot in LiveSlots)
            {
                if (slot.IsEmpty)
                {
                    space += itemInstance.BaseItem.MaxStackSize;
                }
                else if (slot.HeldItem.IsStackableWith(itemInstance))
                {
                    space += (itemInstance.BaseItem.MaxStackSize - slot.Count);
                }
            }
            return space;
        }

        private void EnsureSlotItemStates()
        {
            if (LiveSlots == null)
                return;

            for (int i = 0; i < LiveSlots.Count; i++)
            {
                InventorySlot slot = LiveSlots[i];
                if (slot == null || slot.IsEmpty || slot.HeldItem?.BaseItem == null)
                    continue;

                EnsureItemInstanceStates(slot.HeldItem);
            }
        }

        private static void EnsureItemInstanceStates(ItemInstance itemInstance)
        {
            if (itemInstance == null || itemInstance.BaseItem == null || itemInstance.BaseItem.Components == null)
                return;

            if (itemInstance.States == null)
                itemInstance.States = new List<ItemComponentState>();

            for (int i = 0; i < itemInstance.BaseItem.Components.Count; i++)
            {
                ItemComponent component = itemInstance.BaseItem.Components[i];
                if (component == null)
                    continue;

                ItemComponentState initialState = component.CreateInitialState();
                if (initialState == null)
                    continue;

                bool hasMatchingState = false;
                for (int stateIndex = 0; stateIndex < itemInstance.States.Count; stateIndex++)
                {
                    ItemComponentState existingState = itemInstance.States[stateIndex];
                    if (existingState != null && existingState.GetType() == initialState.GetType())
                    {
                        hasMatchingState = true;
                        break;
                    }
                }

                if (!hasMatchingState)
                    itemInstance.States.Add(initialState);
            }
        }

#if UNITY_EDITOR
        private void ValidateSceneSlotStacking()
        {
            if (!gameObject.scene.IsValid() || LiveSlots == null)
                return;

            for (int i = 0; i < LiveSlots.Count; i++)
            {
                InventorySlot slot = LiveSlots[i];
                if (slot == null || slot.IsEmpty || slot.HeldItem?.BaseItem == null || slot.Count <= 1)
                    continue;

                if (!slot.HeldItem.BaseItem.TryGetStackingValidationMessage(out string validationMessage))
                    continue;

                Debug.LogWarning(
                    $"[InventoryManager] Slot {i} on '{name}' contains Count={slot.Count} for '{slot.HeldItem.BaseItem.ItemName}'. {validationMessage}",
                    this);
            }
        }
#endif
    }
}
