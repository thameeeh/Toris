using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using OutlandHaven.UIToolkit;

namespace OutlandHaven.Inventory
{
    public class PlayerPotionHUDView : IDisposable
    {
        private VisualElement _topElement;
        private VisualTreeAsset _slotTemplate;
        private UIInventoryEventsSO _uiInventoryEvents;

        private Dictionary<InventorySlot, InventorySlotView> _potionSlotDictionary = new Dictionary<InventorySlot, InventorySlotView>();

        private VisualElement _slotPotion1Container;
        private VisualElement _slotPotion2Container;

        private InventoryManager _potionInventory;
        private bool _eventsBound = false;

        public PlayerPotionHUDView(VisualElement topElement, VisualTreeAsset slotTemplate, UIInventoryEventsSO uiInventoryEvents)
        {
            _topElement = topElement;
            _slotTemplate = slotTemplate;
            _uiInventoryEvents = uiInventoryEvents;

            SetVisualElements();
        }

        private void SetVisualElements()
        {
            _slotPotion1Container = _topElement.Q<VisualElement>("hud__potion-slot-1");
            _slotPotion2Container = _topElement.Q<VisualElement>("hud__potion-slot-2");
        }

        public void Setup(InventoryManager potionInventory)
        {
            _potionInventory = potionInventory;
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

        private void HandleSpecificSlotsUpdated(InventorySlot sourceSlot, InventorySlot targetSlot)
        {
            if (sourceSlot != null && _potionSlotDictionary.TryGetValue(sourceSlot, out var sourceView))
            {
                sourceView.Update(sourceSlot);
            }

            if (targetSlot != null && _potionSlotDictionary.TryGetValue(targetSlot, out var targetView))
            {
                targetView.Update(targetSlot);
            }
        }

        private void RefreshSlots()
        {
            if (_potionInventory == null || _potionInventory.LiveSlots == null) return;

            _potionSlotDictionary.Clear();

            RefreshSingleSlot(0, _slotPotion1Container);
            RefreshSingleSlot(1, _slotPotion2Container);
        }

        private void RefreshSingleSlot(int index, VisualElement containerRoot)
        {
            if (containerRoot == null) return;

            // Note: We don't Clear() containerRoot because it has the hotkey label child we want to keep
            // Find or create the slot content container
            VisualElement content = containerRoot.Q<VisualElement>("potion-content");
            if (content == null)
            {
                content = new VisualElement();
                content.name = "potion-content";
                content.style.flexGrow = 1;
                content.pickingMode = PickingMode.Ignore;
                containerRoot.Add(content);
            }
            content.Clear();

            if (index >= _potionInventory.LiveSlots.Count)
                return;

            InventorySlot slotData = _potionInventory.LiveSlots[index];

            TemplateContainer slotInstance = _slotTemplate.Instantiate();
            slotInstance.pickingMode = PickingMode.Ignore;
            slotInstance.style.flexGrow = 1;
            slotInstance.AddToClassList("item-slot--potion");
            content.Add(slotInstance);

            var slotView = new InventorySlotView(slotInstance, _potionInventory);

            // Hotbar items are for display and quick-action, but we still support drag-and-drop to it
            slotView.OnLocalMoveItemRequested += (sourceContainer, sourceSlot, targetContainer, targetSlot, amountToMove) => _uiInventoryEvents.OnRequestMoveItem?.Invoke(sourceContainer, sourceSlot, targetContainer, targetSlot, amountToMove);
            
            // Allow right-click usage from the HUD too
            slotView.OnLocalRightClicked += (slot) => _uiInventoryEvents.OnRequestUse?.Invoke(slot);

            slotView.Update(slotData);

            _potionSlotDictionary.Add(slotData, slotView);
        }

        public void Dispose()
        {
            Hide();
        }
    }
}
