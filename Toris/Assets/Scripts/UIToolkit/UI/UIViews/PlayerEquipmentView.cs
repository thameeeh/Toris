using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using OutlandHaven.UIToolkit;

namespace OutlandHaven.Inventory
{
    public class PlayerEquipmentView : IDisposable
    {
        private VisualElement _topElement;
        private VisualTreeAsset _slotTemplate;
        private UIInventoryEventsSO _uiInventoryEvents;

        private Dictionary<InventorySlot, InventorySlotView> _equipmentSlotDictionary = new Dictionary<InventorySlot, InventorySlotView>();

        // Visual elements representing specific equipment slots
        private VisualElement _slotHeadContainer;
        private VisualElement _slotChestContainer;
        private VisualElement _slotLegsContainer;
        private VisualElement _slotArmsContainer;
        private VisualElement _slotWeaponContainer;

        private InventoryManager _equipmentInventory; // Expected to be provided via Setup
        private bool _eventsBound = false;

        public PlayerEquipmentView(VisualElement topElement, VisualTreeAsset slotTemplate, UIInventoryEventsSO uiInventoryEvents)
        {
            _topElement = topElement;
            _slotTemplate = slotTemplate;
            _uiInventoryEvents = uiInventoryEvents;

            SetVisualElements();
        }

        private void SetVisualElements()
        {
            _slotHeadContainer = _topElement.Q<VisualElement>("slot-head");
            _slotChestContainer = _topElement.Q<VisualElement>("slot-chest");
            _slotLegsContainer = _topElement.Q<VisualElement>("slot-legs");
            _slotArmsContainer = _topElement.Q<VisualElement>("slot-arms");
            _slotWeaponContainer = _topElement.Q<VisualElement>("slot-weapon");
        }

        public void Initialize()
        {
            // Usually anything initial related goes here
        }

        public void Setup(InventoryManager equipmentInventory)
        {
            _equipmentInventory = equipmentInventory;
            RefreshSlots();
        }

        public void Show()
        {
            if (!_eventsBound && _uiInventoryEvents != null)
            {
                _uiInventoryEvents.OnInventoryUpdated += OnInventoryUpdated;

                _uiInventoryEvents.OnSpecificSlotsUpdated += HandleSpecificSlotsUpdated;
                _eventsBound = true;
            }
            RefreshSlots();
        }

        public void Hide()
        {
            if (_eventsBound && _uiInventoryEvents != null)
            {
                _uiInventoryEvents.OnInventoryUpdated -= OnInventoryUpdated;

                _uiInventoryEvents.OnSpecificSlotsUpdated -= HandleSpecificSlotsUpdated;
                _eventsBound = false;
            }
        }

        private void OnInventoryUpdated()
        {
            RefreshSlots();
        }

        // NEW: Method to handle targeted updates
        private void HandleSpecificSlotsUpdated(InventorySlot sourceSlot, InventorySlot targetSlot)
        {
            // Check if the source slot is an equipment slot
            if (sourceSlot != null && _equipmentSlotDictionary.TryGetValue(sourceSlot, out var sourceView))
            {
                sourceView.Update(sourceSlot);
            }

            // Check if the target slot is an equipment slot
            if (targetSlot != null && _equipmentSlotDictionary.TryGetValue(targetSlot, out var targetView))
            {
                targetView.Update(targetSlot);
            }
        }

        private void RefreshSlots()
        {
            if (_equipmentInventory == null || _equipmentInventory.LiveSlots == null) return;

            _equipmentSlotDictionary.Clear();

            // Mapping from index to named container (hardcoded as specified)
            RefreshSingleSlot(0, _slotHeadContainer);
            RefreshSingleSlot(1, _slotChestContainer);
            RefreshSingleSlot(2, _slotLegsContainer);
            RefreshSingleSlot(3, _slotArmsContainer);
            RefreshSingleSlot(4, _slotWeaponContainer);
        }

        private void RefreshSingleSlot(int index, VisualElement containerRoot)
        {
            if (containerRoot == null) return;

            // Clear existing
            containerRoot.Clear();

            if (index >= _equipmentInventory.LiveSlots.Count)
                return;

            InventorySlot slotData = _equipmentInventory.LiveSlots[index];

            TemplateContainer slotInstance = _slotTemplate.Instantiate();
            containerRoot.Add(slotInstance);

            var slotView = new InventorySlotView(slotInstance, _equipmentInventory);

            slotView.OnLocalClicked += (slot) => { if (slot != null && !slot.IsEmpty && slot.HeldItem?.BaseItem != null) {
                    var equipable = slot.HeldItem.BaseItem.GetComponent<EquipableComponent>();
                    if (equipable != null) {
                        _uiInventoryEvents.OnRequestUnequip?.Invoke(equipable.TargetSlot);
                    }
                } 
            };

            slotView.OnLocalRightClicked += (slot) => {
                // The equipment system always interprets right clicks as unequips, ignoring context.
                if (slot != null && !slot.IsEmpty && slot.HeldItem?.BaseItem != null) {
                    var equipable = slot.HeldItem.BaseItem.GetComponent<EquipableComponent>();
                    if (equipable != null) {
                        _uiInventoryEvents.OnRequestUnequip?.Invoke(equipable.TargetSlot);
                    }
                }
            };
            slotView.OnLocalMoveItemRequested += (sourceContainer, sourceSlot, targetContainer, targetSlot) => _uiInventoryEvents.OnRequestMoveItem?.Invoke(sourceContainer, sourceSlot, targetContainer, targetSlot);
            slotView.OnLocalSelectForProcessingRequested += (slot, proxyID) => _uiInventoryEvents.OnRequestSelectForProcessing?.Invoke(slot, proxyID);
            slotView.Update(slotData);

            // Hide amount text label for equipment slots if it's there
            var countLabel = slotInstance.Q<Label>("count-label");
            if (countLabel != null)
            {
                countLabel.style.display = DisplayStyle.None;
            }
            // Click events are now handled natively inside InventorySlotView via PointerUpEvent

            _equipmentSlotDictionary.Add(slotData, slotView);
        }

        public void Dispose()
        {
            if (_eventsBound && _uiInventoryEvents != null)
            {
                _uiInventoryEvents.OnInventoryUpdated -= OnInventoryUpdated;

                _uiInventoryEvents.OnSpecificSlotsUpdated -= HandleSpecificSlotsUpdated;
                _eventsBound = false;
            }
        }
    }
}