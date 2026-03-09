using UnityEngine;
using UnityEngine.UIElements;

namespace OutlandHaven.UIToolkit
{
    public class SmithView : GameView
    {
        public override ScreenType ID => ScreenType.Smith;

        private VisualTreeAsset _slotTemplate;
        private VisualTreeAsset _shopTemplate;
        private UIInventoryEventsSO _uiInventoryEvents;
        private GameSessionSO _gameSession;
        private InventoryContainerSO _shopContainer;

        private VisualElement _middlePanel;

        // SubViews
        private ShopSubView _shopSubView;

        public SmithView(VisualElement topElement, VisualTreeAsset slotTemplate, VisualTreeAsset shopTemplate, UIEventsSO uiEvents, UIInventoryEventsSO uiInventoryEvents, GameSessionSO gameSession, InventoryContainerSO shopContainer)
            : base(topElement, uiEvents)
        {
            _slotTemplate = slotTemplate;
            _shopTemplate = shopTemplate;
            _uiInventoryEvents = uiInventoryEvents;
            _gameSession = gameSession;
            _shopContainer = shopContainer;
        }

        protected override void SetVisualElements()
        {
            _middlePanel = m_TopElement.Q<VisualElement>("Smith-middle__panel");

            // Setup Tab Buttons (Forge, Market, Salvage)
            var labels = m_TopElement.Query<Label>(className: "NPC_PanelButtons").ToList();
            foreach (var label in labels)
            {
                if (label.text == "Market")
                {
                    label.RegisterCallback<ClickEvent>(evt => ShowMarketTab());
                }
            }
        }

        public override void Setup(object payload) 
        {
            // Default to showing Market for now
            ShowMarketTab();
        }

        private void ShowMarketTab()
        {
            if (_middlePanel == null) return;

            // Hide other sub-views when implemented

            // Lazy initialization of the ShopSubView
            if (_shopSubView == null)
            {
                if (_shopTemplate != null)
                {
                    TemplateContainer shopInstance = _shopTemplate.Instantiate();
                    _middlePanel.Add(shopInstance);
                    _shopSubView = new ShopSubView(shopInstance, _slotTemplate, _shopContainer, _uiInventoryEvents, _gameSession);
                    _shopSubView.Initialize();
                }
            }
            
            _shopSubView?.Setup();
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