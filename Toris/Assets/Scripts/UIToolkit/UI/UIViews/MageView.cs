using UnityEngine;
using UnityEngine.UIElements;

namespace OutlandHaven.UIToolkit
{
    public class MageView : GameView
    {
        public override ScreenType ID => ScreenType.Mage;

        private UIInventoryEventsSO _uiInventoryEvents;
        private GameSessionSO _gameSession;
        private InventoryContainerSO _shopContainer;
        private ShopManagerSO _shopManager;

        public MageView(VisualElement topElement, UIEventsSO uiEvents, UIInventoryEventsSO uiInventoryEvents, GameSessionSO gameSession, InventoryContainerSO shopContainer, ShopManagerSO shopManager)
            : base(topElement, uiEvents)
        {
            _uiInventoryEvents = uiInventoryEvents;
            _gameSession = gameSession;
            _shopContainer = shopContainer;
            _shopManager = shopManager;
        }

        protected override void SetVisualElements()
        {
            // Skeleton: Extract visual elements here when UXML is created
            // Example: _mainPanel = m_TopElement.Q<VisualElement>("Mage-main__panel");
        }

        public override void Setup(object payload)
        {
            // Payload could be a specific NPC's inventory
            if (payload is InventoryContainerSO dynamicShopContainer)
            {
                _shopContainer = dynamicShopContainer;
            }

            // Skeleton: Initialize subviews or dynamic content here
        }

        public override void Hide()
        {
            base.Hide();
            // Skeleton: Hide subviews here
        }

        public override void Dispose()
        {
            base.Dispose();
            // Skeleton: Dispose subviews here
        }
    }
}