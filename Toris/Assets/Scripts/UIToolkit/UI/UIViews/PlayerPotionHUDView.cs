using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using OutlandHaven.Inventory;
using OutlandHaven.Tutorial;

namespace OutlandHaven.UIToolkit
{
    public class PlayerPotionHUDView : IDisposable
    {
        private const string HudPotionSlotsTutorialAnchorId = "hud.potion_slots";

        private VisualElement _topElement;
        private VisualTreeAsset _slotTemplate;
        private UIInventoryEventsSO _uiInventoryEvents;

        private Dictionary<InventorySlot, InventorySlotView> _slotDictionary = new Dictionary<InventorySlot, InventorySlotView>();

        private VisualElement _potionBarContainer;
        private VisualElement _slot1Container;
        private VisualElement _slot2Container;

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
            _potionBarContainer = _topElement.Q<VisualElement>("hud__potion-bar");
            _slot1Container = _topElement.Q<VisualElement>("hud__potion-slot-1");
            _slot2Container = _topElement.Q<VisualElement>("hud__potion-slot-2");

            // Cross-system boundary: HUD exposes the hotkey slot bounds only.
            // The tutorial flow owns when and why this anchor is highlighted.
            TutorialAnchorRegistry.Register(HudPotionSlotsTutorialAnchorId, _potionBarContainer);
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
            if (sourceSlot != null && _slotDictionary.TryGetValue(sourceSlot, out var sourceView))
            {
                sourceView.Update(sourceSlot);
            }

            if (targetSlot != null && _slotDictionary.TryGetValue(targetSlot, out var targetView))
            {
                targetView.Update(targetSlot);
            }
        }

        private void RefreshSlots()
        {
            if (_potionInventory == null || _potionInventory.LiveSlots == null) return;

            _slotDictionary.Clear();

            RefreshSingleSlot(0, _slot1Container);
            RefreshSingleSlot(1, _slot2Container);
        }

        private void RefreshSingleSlot(int index, VisualElement containerRoot)
        {
            if (containerRoot == null) return;

            // Clear previous items but keep the hotkey label if it exists
            // Or just clear and rely on the template? 
            // In UXML I added a label. Let's preserve it or re-add it.
            Label hotkeyLabel = containerRoot.Q<Label>(className: "hud-potion-hotkey");
            containerRoot.Clear();
            if (hotkeyLabel != null) containerRoot.Add(hotkeyLabel);

            if (index >= _potionInventory.LiveSlots.Count)
                return;

            InventorySlot slotData = _potionInventory.LiveSlots[index];

            TemplateContainer slotInstance = _slotTemplate.Instantiate();
            slotInstance.pickingMode = PickingMode.Ignore;
            slotInstance.style.flexGrow = 1;
            slotInstance.AddToClassList("item-slot--potion");
            containerRoot.Add(slotInstance);

            var slotView = new InventorySlotView(slotInstance, _potionInventory, _uiInventoryEvents);

            // Important: Right click on HUD also uses the potion
            slotView.OnLocalRightClicked += (slot) => _uiInventoryEvents.OnRequestUse?.Invoke(slot);
            
            // Drag and drop support on HUD
            slotView.OnLocalMoveItemRequested += (sourceContainer, sourceSlot, targetContainer, targetSlot, amountToMove) => 
                _uiInventoryEvents.OnRequestMoveItem?.Invoke(sourceContainer, sourceSlot, targetContainer, targetSlot, amountToMove);

            slotView.OnLocalDragStarted += (sprite, pos, size) => _uiInventoryEvents.OnGlobalDragStarted?.Invoke(sprite, pos, size);
            slotView.OnLocalDragUpdated += (pos) => _uiInventoryEvents.OnGlobalDragUpdated?.Invoke(pos);
            slotView.OnLocalDragStopped += () => _uiInventoryEvents.OnGlobalDragStopped?.Invoke();
            
            slotView.Update(slotData);

            _slotDictionary.Add(slotData, slotView);
        }

        public void Dispose()
        {
            TutorialAnchorRegistry.Unregister(HudPotionSlotsTutorialAnchorId, _potionBarContainer);
            Hide();
        }
    }
}
