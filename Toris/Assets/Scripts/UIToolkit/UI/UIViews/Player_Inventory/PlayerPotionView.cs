using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using OutlandHaven.UIToolkit;

namespace OutlandHaven.Inventory
{
    public class PlayerPotionView : IDisposable
    {
        private VisualElement _topElement;
        private VisualTreeAsset _slotTemplate;
        private UIInventoryEventsSO _uiInventoryEvents;

        private Dictionary<InventorySlot, InventorySlotView> _potionSlotDictionary = new Dictionary<InventorySlot, InventorySlotView>();

        private VisualElement _slotPotion1Container;
        private VisualElement _slotPotion2Container;

        private InventoryManager _potionInventory;
        private bool _eventsBound = false;

        public PlayerPotionView(VisualElement topElement, VisualTreeAsset slotTemplate, UIInventoryEventsSO uiInventoryEvents)
        {
            _topElement = topElement;
            _slotTemplate = slotTemplate;
            _uiInventoryEvents = uiInventoryEvents;

            SetVisualElements();
        }

        private void SetVisualElements()
        {
            _slotPotion1Container = _topElement.Q<VisualElement>("slot-potion-1");
            _slotPotion2Container = _topElement.Q<VisualElement>("slot-potion-2");
        }

        public void Initialize()
        {
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

            containerRoot.Clear();

            if (index >= _potionInventory.LiveSlots.Count)
                return;

            InventorySlot slotData = _potionInventory.LiveSlots[index];

            TemplateContainer slotInstance = _slotTemplate.Instantiate();
            slotInstance.AddToClassList("item-slot--potion");
            containerRoot.Add(slotInstance);

            var slotView = new InventorySlotView(slotInstance, _potionInventory);

            slotView.OnLocalMoveItemRequested += (sourceContainer, sourceSlot, targetContainer, targetSlot, amountToMove) => _uiInventoryEvents.OnRequestMoveItem?.Invoke(sourceContainer, sourceSlot, targetContainer, targetSlot, amountToMove);
            slotView.OnLocalSelectForProcessingRequested += (slot, proxyID) => _uiInventoryEvents.OnRequestSelectForProcessing?.Invoke(slot, proxyID);

            slotView.OnLocalDragStarted += (sprite, pos, size) => _uiInventoryEvents.OnGlobalDragStarted?.Invoke(sprite, pos, size);
            slotView.OnLocalDragUpdated += (pos) => _uiInventoryEvents.OnGlobalDragUpdated?.Invoke(pos);
            slotView.OnLocalDragStopped += () => _uiInventoryEvents.OnGlobalDragStopped?.Invoke();
            slotView.Update(slotData);

            _potionSlotDictionary.Add(slotData, slotView);
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
