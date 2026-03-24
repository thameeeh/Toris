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
                _eventsBound = true;
            }
            RefreshSlots();
        }

        public void Hide()
        {
            if (_eventsBound && _uiInventoryEvents != null)
            {
                _uiInventoryEvents.OnInventoryUpdated -= OnInventoryUpdated;
                _eventsBound = false;
            }
        }

        private void OnInventoryUpdated()
        {
            RefreshSlots();
        }

        private void RefreshSlots()
        {
            if (_equipmentInventory == null || _equipmentInventory.LiveSlots == null) return;

            // Mapping from index to named container (hardcoded as specified)
            // Index 0 = Head, 1 = Chest, 2 = Legs, 3 = Arms, 4 = Weapon
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

            var slotView = new InventorySlotView(slotInstance);
            slotView.Update(slotData);

            // Hide amount text label for equipment slots if it's there
            var countLabel = slotInstance.Q<Label>("count-label");
            if (countLabel != null)
            {
                countLabel.style.display = DisplayStyle.None;
            }

            var currentSlotData = slotData;
            EquipmentSlot slotType = (EquipmentSlot)index;

            slotInstance.RegisterCallback<MouseUpEvent>(evt =>
            {
                if (evt.button == 0) // Left click
                {
                    if (currentSlotData != null && !currentSlotData.IsEmpty)
                    {
                        _uiInventoryEvents?.OnRequestUnequip?.Invoke(slotType);
                    }
                }
            });
        }

        public void Dispose()
        {
            if (_eventsBound && _uiInventoryEvents != null)
            {
                _uiInventoryEvents.OnInventoryUpdated -= OnInventoryUpdated;
                _eventsBound = false;
            }
        }
    }
}