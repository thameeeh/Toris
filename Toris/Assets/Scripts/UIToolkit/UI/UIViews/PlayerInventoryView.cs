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

        private UIInventoryEventsSO _uiInventoryEvents;
        private bool _eventsBound = false;

        public PlayerInventoryView(VisualElement topElement, VisualTreeAsset slotTemplate, GameSessionSO session, UIEventsSO uiEvents, UIInventoryEventsSO uiInventoryEvents)
            : base(topElement, uiEvents)
        {
            _slotTemplate = slotTemplate;
            _gameSession = session;
            _uiInventoryEvents = uiInventoryEvents;
        }

        public override void Show()
        {
            base.Show();
            if (!_eventsBound && _uiInventoryEvents != null)
            {
                _uiInventoryEvents.OnInventoryUpdated += OnInventoryUpdated;
                _eventsBound = true;
            }
        }

        public override void Hide()
        {
            base.Hide();
            if (_eventsBound && _uiInventoryEvents != null)
            {
                _uiInventoryEvents.OnInventoryUpdated -= OnInventoryUpdated;
                _eventsBound = false;
            }
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
        }
        

        private void RefreshGrid(VisualElement gridRoot, InventoryContainerSO data)
        {
            if (gridRoot == null) return;

            // Clear any existing slots (visuals)
            gridRoot.Clear();

            if (data == null || data.Slots == null) return;

            // Loop through data and create visuals
            foreach (var slotData in data.Slots)
            {
                // Instantiate the UXML Template
                TemplateContainer slotInstance = _slotTemplate.Instantiate();
                gridRoot.Add(slotInstance);

                // Initialize the wrapper and update it
                var slotView = new InventorySlotView(slotInstance);
                slotView.Update(slotData);

                // Register click event
                var currentSlotData = slotData; // Capture variable for lambda
                slotInstance.RegisterCallback<MouseUpEvent>(evt =>
                {
                    if (evt.button == 0) // Left click
                    {
                        if (currentSlotData != null && !currentSlotData.IsEmpty)
                        {
                            _uiInventoryEvents?.OnItemClicked?.Invoke(currentSlotData);
                        }
                    }
                });
            }
        }

        void OnDispose()
        {
            _uiInventoryEvents.OnInventoryUpdated -= OnInventoryUpdated;
        }
    }
}