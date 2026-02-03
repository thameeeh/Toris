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

        public InventoryView(VisualElement topElement, VisualTreeAsset slotTemplate, GameSessionSO session, UIEventsSO uiEvents)
            : base(topElement, uiEvents)
        {
            _slotTemplate = slotTemplate;
            _gameSession = session;
        }

        protected override void SetVisualElements()
        {
            // Find the grids where slots live
            _playerGrid = m_TopElement.Q<VisualElement>("grid-player");
        }

        /*
        public override void Setup(object payload)
        {

            RefreshGrid(_playerGrid, _gameSession.PlayerInventory);

            // Handle Payload (Chest/Vendor)
            if (payload is InventoryContainerSO externalData)
            {
                _externalPanel.style.display = DisplayStyle.Flex;
                if (_externalHeader != null) _externalHeader.text = externalData.name;
                RefreshGrid(_externalGrid, externalData);
            }
            else
            { 
                _externalPanel.style.display = DisplayStyle.None;
            }
        }
        */

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