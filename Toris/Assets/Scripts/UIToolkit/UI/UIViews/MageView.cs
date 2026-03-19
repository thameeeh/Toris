using UnityEngine;
using UnityEngine.UIElements;
using OutlandHaven.Inventory;

namespace OutlandHaven.UIToolkit
{
    public class MageView : GameView
    {
        public override ScreenType ID => ScreenType.Mage;

        private VisualTreeAsset _slotTemplate;
        private VisualTreeAsset _shopTemplate;
        private UIInventoryEventsSO _uiInventoryEvents;
        private GameSessionSO _gameSession;
        private InventoryManager _shopContainer;
        private ShopManagerSO _shopManager;

        private VisualElement _middlePanel;

        // SubViews
        private ShopSubView _shopSubView;

        public MageView(VisualElement topElement, VisualTreeAsset slotTemplate, VisualTreeAsset shopTemplate, UIEventsSO uiEvents, UIInventoryEventsSO uiInventoryEvents, GameSessionSO gameSession, ShopManagerSO shopManager)
            : base(topElement, uiEvents)
        {
            _slotTemplate = slotTemplate;
            _shopTemplate = shopTemplate;
            _uiInventoryEvents = uiInventoryEvents;
            _gameSession = gameSession;
            _shopManager = shopManager;
        }

        protected override void SetVisualElements()
        {
            _middlePanel = m_TopElement.Q<VisualElement>("Mage-middle__panel");

            // Setup Tab Buttons (Market)
            var marketTab = m_TopElement.Q<VisualElement>("Mage_Market--Tab");
            if (marketTab != null) marketTab.RegisterCallback<ClickEvent>(evt => ShowMarketTab());
        }

        public override void Setup(object payload)
        {
            // Payload could be a specific NPC's inventory
            if (payload is InventoryManager dynamicShopContainer)
            {
                _shopContainer = dynamicShopContainer;
            }

            // Default to showing Market for now
            ShowMarketTab();
        }

        private void ShowMarketTab()
        {
            if (_middlePanel == null) return;

            // Lazy initialization of the ShopSubView
            if (_shopSubView == null)
            {
                if (_shopTemplate != null)
                {
                    TemplateContainer shopInstance = _shopTemplate.Instantiate();
                    _middlePanel.Add(shopInstance);
                    _shopSubView = new ShopSubView(shopInstance, _slotTemplate, _uiInventoryEvents, _gameSession);
                    _shopSubView.Initialize();
                }
            }

            _shopSubView?.Setup(_shopContainer);
            _shopSubView?.Show();
        }

        public override void Hide()
        {
            base.Hide();
            _shopSubView?.Hide();
        }

        public override void Dispose()
        {
            base.Dispose();
            _shopSubView?.Dispose();
        }
    }
}