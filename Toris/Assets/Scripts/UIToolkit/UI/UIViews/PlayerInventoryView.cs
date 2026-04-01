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
            // Find the grid where slots live directly (ScrollView removed in favor of static Flexbox grid)
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

            // Loop through data and dynamically instantiate exactly 21 slots to form a 3x7 static grid
            for (int i = 0; i < data.LiveSlots.Count; i++)
            {
                var slotData = data.LiveSlots[i];

                // Instantiate the UI Toolkit slot template dynamically, rather than relying on hardcoded UXML slots
                TemplateContainer slotInstance = _slotTemplate.Instantiate();
                gridRoot.Add(slotInstance);

                // Initialize the wrapper and update it with actual slot data
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