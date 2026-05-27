using UnityEngine;
using UnityEngine.UIElements;
using OutlandHaven.Inventory;
using System;

namespace OutlandHaven.UIToolkit
{
    public class SageUpgradeView : GameView
    {
        public override ScreenType ID => ScreenType.SageUpgrade;

        private UIInventoryEventsSO _uiInventoryEvents;
        private GameSessionSO _gameSession;
        private UpgradeSalvageManagerSO _upgradeManager;
        private CraftingManagerSO _brewingManager;

        private VisualTreeAsset _upgradeSubViewTemplate;
        private VisualTreeAsset _brewSubViewTemplate;
        private VisualTreeAsset _slotTemplate;

        // Viewports & Tabs
        private VisualElement _middlePanel;
        private VisualElement _upgradeTab;
        private VisualElement _brewingTab;

        private const string ActiveTabClass = "panel-tab--active";

        // SubViews
        private InfusionSubView_Sage _infusionSubView;
        private BrewSubView_Sage _brewSubView;

        public SageUpgradeView(
            VisualElement topElement,
            VisualTreeAsset upgradeSubViewTemplate,
            VisualTreeAsset brewSubViewTemplate,
            VisualTreeAsset slotTemplate,
            UIEventsSO uiEvents,
            UIInventoryEventsSO uiInventoryEvents,
            GameSessionSO gameSession,
            UpgradeSalvageManagerSO upgradeManager,
            CraftingManagerSO brewingManager)
            : base(topElement, uiEvents)
        {
            _upgradeSubViewTemplate = upgradeSubViewTemplate;
            _brewSubViewTemplate = brewSubViewTemplate;
            _slotTemplate = slotTemplate;
            _uiInventoryEvents = uiInventoryEvents;
            _gameSession = gameSession;
            _upgradeManager = upgradeManager;
            _brewingManager = brewingManager;
        }

        protected override void SetVisualElements()
        {
            _middlePanel = m_TopElement.Q<VisualElement>("Sage-middle__panel");

            // Cache Tab Elements
            _upgradeTab = m_TopElement.Q<VisualElement>("Sage_Upgrade--Tab");
            _brewingTab = m_TopElement.Q<VisualElement>("Sage_Brewing--Tab");

            // Register Click Callbacks
            if (_upgradeTab != null) _upgradeTab.RegisterCallback<ClickEvent>(evt => ShowUpgradeTab());
            if (_brewingTab != null) _brewingTab.RegisterCallback<ClickEvent>(evt => ShowBrewingTab());
        }

        public override void Setup(object payload)
        {
            // Default to showing the Infusion (Upgrade) tab
            ShowUpgradeTab();
        }

        private void UpdateActiveTabVisual(VisualElement activeTab)
        {
            // Reset active classes
            _upgradeTab?.RemoveFromClassList(ActiveTabClass);
            _brewingTab?.RemoveFromClassList(ActiveTabClass);

            // Apply to the active tab
            activeTab?.AddToClassList(ActiveTabClass);
        }

        private void ShowUpgradeTab()
        {
            if (_middlePanel == null) return;

            UpdateActiveTabVisual(_upgradeTab);

            _brewSubView?.Hide();

            // Lazy initialization of Infusion SubView
            if (_infusionSubView == null)
            {
                if (_upgradeSubViewTemplate != null)
                {
                    TemplateContainer infusionInstance = _upgradeSubViewTemplate.Instantiate();
                    infusionInstance.style.flexGrow = 1;
                    _middlePanel.Add(infusionInstance);
                    _infusionSubView = new InfusionSubView_Sage(infusionInstance, _slotTemplate, _uiInventoryEvents, _gameSession, _upgradeManager);
                    _infusionSubView.Initialize();
                }
            }

            _infusionSubView?.Setup();
            _infusionSubView?.Show();
        }

        private void ShowBrewingTab()
        {
            if (_middlePanel == null) return;

            UpdateActiveTabVisual(_brewingTab);

            _infusionSubView?.Hide();

            // Lazy initialization of Brewing SubView
            if (_brewSubView == null)
            {
                if (_brewSubViewTemplate != null)
                {
                    TemplateContainer brewInstance = _brewSubViewTemplate.Instantiate();
                    brewInstance.style.flexGrow = 1;
                    _middlePanel.Add(brewInstance);
                    _brewSubView = new BrewSubView_Sage(brewInstance, _slotTemplate, _uiInventoryEvents, _brewingManager);
                    _brewSubView.Initialize();
                }
            }

            _brewSubView?.Setup();
            _brewSubView?.Show();
        }

        public override void Show()
        {
            base.Show();
            
            // Show the default active subview
            _infusionSubView?.Show();
            _brewSubView?.Show();
        }

        public override void Hide()
        {
            base.Hide();
            _infusionSubView?.Hide();
            _brewSubView?.Hide();
        }

        public override void Dispose()
        {
            base.Dispose();
            _infusionSubView?.Dispose();
            _brewSubView?.Dispose();
        }
    }
}
