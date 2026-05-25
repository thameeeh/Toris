using System;
using UnityEngine.UIElements;
using OutlandHaven.UIToolkit;

namespace OutlandHaven.UIToolkit
{
    public class PauseMenuView : GameView
    {
        public override ScreenType ID => ScreenType.PauseMenu;

        private Button _resumeButton;
        private Button _adventureLogButton;
        private Button _settingsButton;
        private Button _mainMenuButton;
        private Button _toggleItemsButton;

        private VisualElement _statsSubPanel;
        private VisualElement _itemsSubPanel;
        private Label _playTimeLabel;
        private Label _totalKillsLabel;
        private Label _wolfKillsLabel;
        private Label _totalPickUpsLabel;
        private ScrollView _itemsListScrollView;

        public event Action OnResumeClicked;
        public event Action OnAdventureLogClicked;
        public event Action OnSettingsClicked;
        public event Action OnMainMenuClicked;
        public event Action OnToggleItemsClicked;

        public PauseMenuView(VisualElement topElement, UIEventsSO uiEvents) : base(topElement, uiEvents) { }

        public override void Initialize()
        {
            base.Initialize();

            _resumeButton = Root.Q<Button>("Btn_Resume");
            _adventureLogButton = Root.Q<Button>("Btn_AdventureLog");
            _settingsButton = Root.Q<Button>("Btn_Settings");
            _mainMenuButton = Root.Q<Button>("Btn_MainMenu");
            _toggleItemsButton = Root.Q<Button>("Btn_ToggleItemsList");

            _statsSubPanel = Root.Q<VisualElement>("StatsSubPanel");
            _itemsSubPanel = Root.Q<VisualElement>("ItemsSubPanel");
            _playTimeLabel = Root.Q<Label>("Lbl_PlayTime");
            _totalKillsLabel = Root.Q<Label>("Lbl_TotalKills");
            _wolfKillsLabel = Root.Q<Label>("Lbl_WolfKills");
            _totalPickUpsLabel = Root.Q<Label>("Lbl_TotalPickUps");
            _itemsListScrollView = Root.Q<ScrollView>("Scr_ItemsList");

            if (_resumeButton != null) _resumeButton.clicked += HandleResumeClicked;
            if (_adventureLogButton != null) _adventureLogButton.clicked += HandleAdventureLogClicked;
            if (_settingsButton != null) _settingsButton.clicked += HandleSettingsClicked;
            if (_mainMenuButton != null) _mainMenuButton.clicked += HandleMainMenuClicked;
            if (_toggleItemsButton != null) _toggleItemsButton.clicked += HandleToggleItemsClicked;
        }

        private void HandleResumeClicked() => OnResumeClicked?.Invoke();
        private void HandleAdventureLogClicked() => OnAdventureLogClicked?.Invoke();
        private void HandleSettingsClicked() => OnSettingsClicked?.Invoke();
        private void HandleMainMenuClicked() => OnMainMenuClicked?.Invoke();
        private void HandleToggleItemsClicked() => OnToggleItemsClicked?.Invoke();

        public void SetStatsPanelActive(bool active)
        {
            if (_statsSubPanel != null)
            {
                _statsSubPanel.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        public void SetItemsPanelActive(bool active)
        {
            if (_itemsSubPanel != null)
            {
                _itemsSubPanel.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        public void PopulateStats(
            int totalKills,
            int wolfKills,
            string playtimeString,
            int totalPickUps,
            System.Collections.Generic.Dictionary<string, int> itemPickUps)
        {
            if (_playTimeLabel != null) _playTimeLabel.text = playtimeString;
            if (_totalKillsLabel != null) _totalKillsLabel.text = totalKills.ToString();
            if (_wolfKillsLabel != null) _wolfKillsLabel.text = wolfKills.ToString();
            if (_totalPickUpsLabel != null) _totalPickUpsLabel.text = totalPickUps.ToString();

            if (_itemsListScrollView != null)
            {
                _itemsListScrollView.Clear();

                if (itemPickUps != null)
                {
                    foreach (var kvp in itemPickUps)
                    {
                        VisualElement row = new VisualElement();
                        row.AddToClassList("stats-row");

                        Label label = new Label(kvp.Key);
                        label.AddToClassList("stats-label");

                        Label value = new Label(kvp.Value.ToString());
                        value.AddToClassList("stats-value");

                        row.Add(label);
                        row.Add(value);

                        _itemsListScrollView.Add(row);
                    }
                }
            }
        }

        public override void Dispose()
        {
            if (_resumeButton != null) _resumeButton.clicked -= HandleResumeClicked;
            if (_adventureLogButton != null) _adventureLogButton.clicked -= HandleAdventureLogClicked;
            if (_settingsButton != null) _settingsButton.clicked -= HandleSettingsClicked;
            if (_mainMenuButton != null) _mainMenuButton.clicked -= HandleMainMenuClicked;
            if (_toggleItemsButton != null) _toggleItemsButton.clicked -= HandleToggleItemsClicked;
            base.Dispose();
        }
    }
}
