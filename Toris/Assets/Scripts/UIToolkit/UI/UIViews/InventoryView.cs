using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace OutlandHaven.UIToolkit
{
    public class InventoryView : GameView
    {
        public override ScreenType ID => ScreenType.Inventory;

        private VisualTreeAsset _slotTemplate;
        private GameSessionSO _gameSession;

        // UI Containers
        private VisualElement _playerGrid;
        private VisualElement _externalGrid;
        private VisualElement _externalPanel; // The whole right side
        private Label _externalHeader;

        public InventoryView(VisualElement topElement, VisualTreeAsset slotTemplate, GameSessionSO session)
            : base(topElement)
        {
            _slotTemplate = slotTemplate;
            _gameSession = session;
        }

        protected override void SetVisualElements()
        {
            // Find the grids where slots live
            _playerGrid = m_TopElement.Q<VisualElement>("grid-player");
            _externalGrid = m_TopElement.Q<VisualElement>("grid-external");

            // Find the containers for toggling visibility
            _externalPanel = m_TopElement.Q<VisualElement>("container__external");
            _externalHeader = m_TopElement.Q<Label>("label__external-header");
        }

        public override void Setup(object payload)
        {
            // 1. Refresh Player Inventory (Always)
            // Assuming GameSession has a generic 'PlayerInventory' field now
            // If you haven't added 'public InventoryContainerSO PlayerInventory' to GameSessionSO, do it!
            RefreshGrid(_playerGrid, _gameSession.PlayerInventory); // You might need to add this field

            // 2. Handle Payload (Chest/Vendor)
            if (payload is InventoryContainerSO externalData)
            {
                _externalPanel.style.display = DisplayStyle.Flex;
                if (_externalHeader != null) _externalHeader.text = externalData.name;
                RefreshGrid(_externalGrid, externalData);
            }
            else
            {
                // No payload? Hide the right panel.
                _externalPanel.style.display = DisplayStyle.None;
            }
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
            }
        }
    }
}