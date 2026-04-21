using UnityEngine;
using UnityEngine.UIElements;
using OutlandHaven.Inventory;

namespace OutlandHaven.UIToolkit
{
    public class SmithView : GameView
    {
        public override ScreenType ID => ScreenType.Smith;

        private VisualTreeAsset _slotTemplate;
        private VisualTreeAsset _shopTemplate;
        private VisualTreeAsset _forgeTemplate;
        private VisualTreeAsset _salvageTemplate;
        private UIInventoryEventsSO _uiInventoryEvents;
        private GameSessionSO _gameSession;
        private PlayerHUDBridge _playerHudBridge;
        private InventoryManager _shopContainer;
        private CraftingManagerSO _craftingManager;
        private SalvageManagerSO _salvageManager;

        private VisualElement _middlePanel;

        private VisualElement _marketTab;
        private VisualElement _forgeTab;
        private VisualElement _salvageTab;

        private const string ActiveTabClass = "panel-tab--active";

        // SubViews
        private ShopSubView _shopSubView;
        private ForgeSubView _forgeSubView;
        private SalvageSubView _salvageSubView;

        public SmithView(VisualElement topElement, VisualTreeAsset slotTemplate, VisualTreeAsset shopTemplate, VisualTreeAsset forgeTemplate, VisualTreeAsset salvageTemplate, UIEventsSO uiEvents, UIInventoryEventsSO uiInventoryEvents, GameSessionSO gameSession, PlayerHUDBridge playerHudBridge, CraftingManagerSO craftingManager, SalvageManagerSO salvageManager)
            : base(topElement, uiEvents)
        {
            _slotTemplate = slotTemplate;
            _shopTemplate = shopTemplate;
            _forgeTemplate = forgeTemplate;
            _salvageTemplate = salvageTemplate;
            _uiInventoryEvents = uiInventoryEvents;
            _gameSession = gameSession;
            _playerHudBridge = playerHudBridge;
            _craftingManager = craftingManager;
            _salvageManager = salvageManager;
        }

        protected override void SetVisualElements()
        {
            _middlePanel = m_TopElement.Q<VisualElement>("Smith-middle__panel");

            // Cache Tab Elements
            _marketTab = m_TopElement.Q<VisualElement>("Smith_Market--Tab");
            _forgeTab = m_TopElement.Q<VisualElement>("Smith_Forge--Tab");
            _salvageTab = m_TopElement.Q<VisualElement>("Smith_Salvage--Tab");

            // Setup Tab Button Callbacks
            if (_marketTab != null) _marketTab.RegisterCallback<ClickEvent>(evt => ShowMarketTab());
            if (_forgeTab != null) _forgeTab.RegisterCallback<ClickEvent>(evt => ShowForgeTab());
            if (_salvageTab != null) _salvageTab.RegisterCallback<ClickEvent>(evt => ShowSalvageTab());
        }

        public override void Setup(object payload) 
        {
            // Payload could be a specific NPC's inventory
            if (payload is InventoryManager dynamicShopContainer)
            {
                _shopContainer = dynamicShopContainer;
            }

            // Default to showing Forge for now
            ShowForgeTab();
        }

        private void UpdateActiveTabVisual(VisualElement activeTab)
        {
            // Remove the active class from all tabs
            _marketTab?.RemoveFromClassList(ActiveTabClass);
            _forgeTab?.RemoveFromClassList(ActiveTabClass);
            _salvageTab?.RemoveFromClassList(ActiveTabClass);

            // Apply it only to the newly selected tab
            activeTab?.AddToClassList(ActiveTabClass);
        }

        private void ShowMarketTab()
        {
            if (_middlePanel == null) return;

            UpdateActiveTabVisual(_marketTab);

            _forgeSubView?.Hide();
            _salvageSubView?.Hide();

            // Lazy initialization of the ShopSubView
            if (_shopSubView == null)
            {
                if (_shopTemplate != null)
                {
                    TemplateContainer shopInstance = _shopTemplate.Instantiate();
                    _middlePanel.Add(shopInstance);
                    _shopSubView = new ShopSubView(shopInstance, _slotTemplate, _uiInventoryEvents, _gameSession, _playerHudBridge);
                    _shopSubView.Initialize();
                }
            }
            
            _shopSubView?.Setup(_shopContainer);
            _shopSubView?.Show();
        }

        private void ShowForgeTab()
        {
            if (_middlePanel == null) return;

            UpdateActiveTabVisual(_forgeTab);

            _shopSubView?.Hide();
            _salvageSubView?.Hide();

            // Lazy initialization of the ForgeSubView
            if (_forgeSubView == null)
            {
                if (_forgeTemplate != null)
                {
                    TemplateContainer forgeInstance = _forgeTemplate.Instantiate();
                    _middlePanel.Add(forgeInstance);
                    _forgeSubView = new ForgeSubView(forgeInstance, _slotTemplate, _uiInventoryEvents, _craftingManager);
                    _forgeSubView.Initialize();
                }
            }

            _forgeSubView?.Setup();
            _forgeSubView?.Show();
        }

        private void ShowSalvageTab()
        {
            if (_middlePanel == null) return;

            UpdateActiveTabVisual(_salvageTab);

            _shopSubView?.Hide();
            _forgeSubView?.Hide();

            // Lazy initialization of the SalvageSubView
            if (_salvageSubView == null)
            {
                if (_salvageTemplate != null)
                {
                    TemplateContainer salvageInstance = _salvageTemplate.Instantiate();
                    _middlePanel.Add(salvageInstance);
                    _salvageSubView = new SalvageSubView(salvageInstance, _slotTemplate, _uiInventoryEvents, _salvageManager);
                    _salvageSubView.Initialize();
                }
            }

            _salvageSubView?.Setup();
            _salvageSubView?.Show();
        }

        public override void Hide()
        {
            base.Hide();
            _shopSubView?.Hide();
            _forgeSubView?.Hide();
            _salvageSubView?.Hide();
        }

        public override void Dispose()
        {
            base.Dispose();
            _shopSubView?.Dispose();
            _forgeSubView?.Dispose();
            _salvageSubView?.Dispose();
        }
    }
}