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

        private VisualElement _statsSubPanel;
        private Label _playTimeLabel;
        private Label _totalKillsLabel;
        private Label _wolfKillsLabel;

        public event Action OnResumeClicked;
        public event Action OnAdventureLogClicked;
        public event Action OnSettingsClicked;
        public event Action OnMainMenuClicked;

        public PauseMenuView(VisualElement topElement, UIEventsSO uiEvents) : base(topElement, uiEvents) { }

        public override void Initialize()
        {
            base.Initialize();

            _resumeButton = Root.Q<Button>("Btn_Resume");
            _adventureLogButton = Root.Q<Button>("Btn_AdventureLog");
            _settingsButton = Root.Q<Button>("Btn_Settings");
            _mainMenuButton = Root.Q<Button>("Btn_MainMenu");

            _statsSubPanel = Root.Q<VisualElement>("StatsSubPanel");
            _playTimeLabel = Root.Q<Label>("Lbl_PlayTime");
            _totalKillsLabel = Root.Q<Label>("Lbl_TotalKills");
            _wolfKillsLabel = Root.Q<Label>("Lbl_WolfKills");

            if (_resumeButton != null) _resumeButton.clicked += HandleResumeClicked;
            if (_adventureLogButton != null) _adventureLogButton.clicked += HandleAdventureLogClicked;
            if (_settingsButton != null) _settingsButton.clicked += HandleSettingsClicked;
            if (_mainMenuButton != null) _mainMenuButton.clicked += HandleMainMenuClicked;
        }

        private void HandleResumeClicked() => OnResumeClicked?.Invoke();
        private void HandleAdventureLogClicked() => OnAdventureLogClicked?.Invoke();
        private void HandleSettingsClicked() => OnSettingsClicked?.Invoke();
        private void HandleMainMenuClicked() => OnMainMenuClicked?.Invoke();

        public void SetStatsPanelActive(bool active)
        {
            if (_statsSubPanel != null)
            {
                _statsSubPanel.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        public void PopulateStats(int totalKills, int wolfKills, string playtimeString)
        {
            if (_playTimeLabel != null) _playTimeLabel.text = playtimeString;
            if (_totalKillsLabel != null) _totalKillsLabel.text = totalKills.ToString();
            if (_wolfKillsLabel != null) _wolfKillsLabel.text = wolfKills.ToString();
        }

        public override void Dispose()
        {
            if (_resumeButton != null) _resumeButton.clicked -= HandleResumeClicked;
            if (_adventureLogButton != null) _adventureLogButton.clicked -= HandleAdventureLogClicked;
            if (_settingsButton != null) _settingsButton.clicked -= HandleSettingsClicked;
            if (_mainMenuButton != null) _mainMenuButton.clicked -= HandleMainMenuClicked;
            base.Dispose();
        }
    }
}
