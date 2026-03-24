using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;
using OutlandHaven.UIToolkit;

namespace OutlandHaven.Inventory
{
    public class PlayerInventoryView : GameView, IDisposable
    {
        public override ScreenType ID => ScreenType.Inventory;

        private VisualTreeAsset _slotTemplate;
        private GameSessionSO _gameSession;

        // UI Containers
        private VisualElement _playerGrid;
        private PlayerEquipmentView _equipmentView;
        private InventoryManager _equipmentInventory;

        private UIInventoryEventsSO _uiInventoryEvents;
        private bool _eventsBound = false;

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

            // Clear any existing slots (visuals)
            gridRoot.Clear();

            if (data == null || data.LiveSlots == null) return;

            // Loop through data and create visuals
            foreach (var slotData in data.LiveSlots)
            {
                // Instantiate the UXML Template
                TemplateContainer slotInstance = _slotTemplate.Instantiate();
                gridRoot.Add(slotInstance);

                // Initialize the wrapper and update it
                // We pass in the owning InventoryManager (data) and the UI events
                var slotView = new InventorySlotView(slotInstance, data, _uiInventoryEvents);
                slotView.Update(slotData);

                // Click events are now handled natively inside InventorySlotView via PointerUpEvent
            }
        }

        public void Dispose()
        {
            if (_eventsBound && _uiInventoryEvents != null)
            {
                _uiInventoryEvents.OnInventoryUpdated -= OnInventoryUpdated;
                _eventsBound = false;
            }
            _equipmentView?.Dispose();
        }
    }
}