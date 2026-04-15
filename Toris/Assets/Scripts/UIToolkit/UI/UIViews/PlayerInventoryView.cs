using OutlandHaven.UIToolkit;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace OutlandHaven.Inventory
{
    public class PlayerInventoryView : GameView, IDisposable
    {
        public override ScreenType ID => ScreenType.Inventory;

        private VisualTreeAsset _slotTemplate;
        private GameSessionSO _gameSession;

        private Dictionary<InventorySlot, InventorySlotView> _slotDictionary = new Dictionary<InventorySlot, InventorySlotView>();

        // UI Containers
        private VisualElement _playerGrid;
        private PlayerEquipmentView _equipmentView;
        private InventoryManager _equipmentInventory;

        private UIInventoryEventsSO _uiInventoryEvents;
        private bool _eventsBound = false;
        private InventoryInteractionContext _currentContext = InventoryInteractionContext.Normal;

        public PlayerInventoryView(VisualElement topElement, VisualTreeAsset slotTemplate, GameSessionSO session, UIEventsSO uiEvents, UIInventoryEventsSO uiInventoryEvents, InventoryManager equipmentInventory = null)
            : base(topElement, uiEvents)
        {
            _slotTemplate = slotTemplate;
            _gameSession = session;
            _uiInventoryEvents = uiInventoryEvents;
            _equipmentInventory = equipmentInventory;
        }

        public override void Initialize()
        {
            base.Initialize();
            _equipmentView = new PlayerEquipmentView(m_TopElement, _slotTemplate, _uiInventoryEvents);
            _equipmentView.Initialize();
        }

        public override void Show()
        {
            base.Show();
            if (!_eventsBound && _uiInventoryEvents != null)
            {
                _uiInventoryEvents.OnInventoryUpdated += OnInventoryUpdated;
                _uiInventoryEvents.OnInteractionContextChanged += HandleContextChanged;

                _uiInventoryEvents.OnSpecificSlotsUpdated += HandleSpecificSlotsUpdated;
                _eventsBound = true;
            }
            _equipmentView?.Show();
        }

        public override void Hide()
        {
            base.Hide();
            if (_eventsBound && _uiInventoryEvents != null)
            {
                _uiInventoryEvents.OnInventoryUpdated -= OnInventoryUpdated;
                _uiInventoryEvents.OnInteractionContextChanged -= HandleContextChanged;

                _uiInventoryEvents.OnSpecificSlotsUpdated -= HandleSpecificSlotsUpdated;
                _eventsBound = false;
            }
            _equipmentView?.Hide();
        }

        protected override void SetVisualElements()
        {
            // Find the grids where slots live
            _playerGrid = m_TopElement.Q<VisualElement>("grid-player");
        }

        void OnInventoryUpdated() 
        {
            RefreshGrid(_playerGrid, _gameSession.PlayerInventory);
        }

        public override void Setup(object payload)
        {
            // Refresh Player Inventory (Always)
            RefreshGrid(_playerGrid, _gameSession.PlayerInventory); 
            _equipmentView?.Setup(_equipmentInventory);
        }

        private void RefreshGrid(VisualElement gridRoot, InventoryManager data)
        {
            if (gridRoot == null) return;
            gridRoot.Clear();

            _slotDictionary.Clear();

            if (data == null || data.LiveSlots == null) return;

            // Loop through data and create visuals
            foreach (var slotData in data.LiveSlots)
            {
                // Instantiate the UXML Template
                TemplateContainer slotInstance = _slotTemplate.Instantiate();
                gridRoot.Add(slotInstance);

                // Initialize the wrapper and update it
                // We pass in the owning InventoryManager (data) and the UI events
                var slotView = new InventorySlotView(slotInstance, data);

                slotView.OnLocalClicked += (slot) => _uiInventoryEvents.OnItemClicked?.Invoke(slot);
                slotView.OnLocalRightClicked += HandleMainInventoryRightClick;
                slotView.OnLocalMoveItemRequested += (sourceContainer, sourceSlot, targetContainer, targetSlot, amountToMove) => _uiInventoryEvents.OnRequestMoveItem?.Invoke(sourceContainer, sourceSlot, targetContainer, targetSlot, amountToMove);
                slotView.OnLocalSelectForProcessingRequested += (slot, proxyID) => _uiInventoryEvents.OnRequestSelectForProcessing?.Invoke(slot, proxyID);

                slotView.OnLocalDragStarted += (sprite, pos, size) => _uiInventoryEvents.OnGlobalDragStarted?.Invoke(sprite, pos, size);
                slotView.OnLocalDragUpdated += (pos) => _uiInventoryEvents.OnGlobalDragUpdated?.Invoke(pos);
                slotView.OnLocalDragStopped += () => _uiInventoryEvents.OnGlobalDragStopped?.Invoke();
                slotView.Update(slotData);
                // Click events are now handled natively inside InventorySlotView via PointerUpEvent
                
                //view is being saved into the dictionary using the data slot as the key
                _slotDictionary.Add(slotData, slotView);
            }
        }

        private void HandleSpecificSlotsUpdated(InventorySlot sourceSlot, InventorySlot targetSlot)
        {
            // If the source slot belongs to this grid, update its visual
            if (sourceSlot != null && _slotDictionary.TryGetValue(sourceSlot, out var sourceView))
            {
                sourceView.Update(sourceSlot);
            }

            // If the target slot belongs to this grid, update its visual
            if (targetSlot != null && _slotDictionary.TryGetValue(targetSlot, out var targetView))
            {
                targetView.Update(targetSlot);
            }
        }

        private void HandleContextChanged(InventoryInteractionContext newContext)
        {
            _currentContext = newContext;
        }

        private void HandleMainInventoryRightClick(InventorySlot dataSlot)
        {
            switch (_currentContext)
            {
                case InventoryInteractionContext.Shop:
                    _uiInventoryEvents.OnRequestSell?.Invoke(dataSlot.HeldItem, 1);
                    break;
                case InventoryInteractionContext.Salvage:
                    _uiInventoryEvents.OnRequestSalvage?.Invoke(dataSlot, SalvageType.Material); // default salvage type
                    break;
                case InventoryInteractionContext.Normal:
                default:
                    _uiInventoryEvents.OnRequestEquip?.Invoke(dataSlot);
                    break;
            }
        }

        public void Dispose()
        {
            if (_eventsBound && _uiInventoryEvents != null)
            {
                _uiInventoryEvents.OnInventoryUpdated -= OnInventoryUpdated;
                _uiInventoryEvents.OnInteractionContextChanged -= HandleContextChanged;
                _eventsBound = false;
            }
            _equipmentView?.Dispose();
        }
    }
}