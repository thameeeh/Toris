using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;
using OutlandHaven.UIToolkit;
using OutlandHaven.Player.Equipment;

namespace OutlandHaven.Inventory
{
    public class PlayerInventoryView : GameView, IDisposable
    {
        public override ScreenType ID => ScreenType.Inventory;

        private VisualTreeAsset _slotTemplate;
        private GameSessionSO _gameSession;

        // UI Containers
        private VisualElement _playerGrid;

        // Equipment Slots
        private Dictionary<EquipmentSlot, EquipmentSlotView> _equipmentSlots;

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
                if (_gameSession.PlayerEquipment != null)
                {
                    _gameSession.PlayerEquipment.OnEquipmentChanged += OnEquipmentChanged;
                }
                _eventsBound = true;
            }
        }

        public override void Hide()
        {
            base.Hide();
            if (_eventsBound && _uiInventoryEvents != null)
            {
                _uiInventoryEvents.OnInventoryUpdated -= OnInventoryUpdated;
                if (_gameSession.PlayerEquipment != null)
                {
                    _gameSession.PlayerEquipment.OnEquipmentChanged -= OnEquipmentChanged;
                }
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

        void OnEquipmentChanged(EquipmentSlot slot, ItemInstance oldItem, ItemInstance newItem)
        {
            if (_equipmentSlots != null && _equipmentSlots.TryGetValue(slot, out var slotView))
            {
                slotView.Update(newItem);
            }
        }

        public override void Setup(object payload)
        {
            // Setup Equipment Slots
            SetupEquipmentSlots();

            // Refresh Player Inventory (Always)
            RefreshGrid(_playerGrid, _gameSession.PlayerInventory);
        }

        private void SetupEquipmentSlots()
        {
            if (_equipmentSlots == null)
            {
                _equipmentSlots = new Dictionary<EquipmentSlot, EquipmentSlotView>();

                var slotNames = new Dictionary<EquipmentSlot, string>
                {
                    { EquipmentSlot.Head, "slot-head" },
                    { EquipmentSlot.Chest, "slot-chest" },
                    { EquipmentSlot.Legs, "slot-legs" },
                    { EquipmentSlot.Arms, "slot-arms" },
                    { EquipmentSlot.Weapon, "slot-weapon" }
                };

                foreach (var kvp in slotNames)
                {
                    var slotContainer = m_TopElement.Q<VisualElement>(kvp.Value);
                    if (slotContainer != null)
                    {
                        slotContainer.Clear();

                        TemplateContainer slotInstance = _slotTemplate.Instantiate();
                        slotContainer.Add(slotInstance);

                        var slotView = new EquipmentSlotView(slotInstance);
                        _equipmentSlots[kvp.Key] = slotView;
                    }
                }
            }

            // Refresh visuals with current equipment
            if (_gameSession.PlayerEquipment != null)
            {
                foreach (var kvp in _equipmentSlots)
                {
                    kvp.Value.Update(_gameSession.PlayerEquipment.GetEquippedItem(kvp.Key));
                }
            }
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
            if (_gameSession.PlayerEquipment != null)
            {
                _gameSession.PlayerEquipment.OnEquipmentChanged -= OnEquipmentChanged;
            }
        }
    }
}