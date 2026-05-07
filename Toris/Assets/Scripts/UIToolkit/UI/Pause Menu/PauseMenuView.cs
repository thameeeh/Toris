using System;
using UnityEngine.UIElements;
using OutlandHaven.UIToolkit;

namespace OutlandHaven.UIToolkit
{
    public class PauseMenuView : GameView
    {
        public override ScreenType ID => ScreenType.PauseMenu;

        private Button _resumeButton;
        private Button _settingsButton;
        private Button _mainMenuButton;

        public event Action OnResumeClicked;
        public event Action OnSettingsClicked;
        public event Action OnMainMenuClicked;

        public PauseMenuView(VisualElement topElement, UIEventsSO uiEvents) : base(topElement, uiEvents) { }

        public override void Initialize()
        {
            base.Initialize();

            _resumeButton = Root.Q<Button>("Btn_Resume");
            _settingsButton = Root.Q<Button>("Btn_Settings");
            _mainMenuButton = Root.Q<Button>("Btn_MainMenu");

            if (_resumeButton != null) _resumeButton.clicked += HandleResumeClicked;
            if (_settingsButton != null) _settingsButton.clicked += HandleSettingsClicked;
            if (_mainMenuButton != null) _mainMenuButton.clicked += HandleMainMenuClicked;
        }

        private void HandleResumeClicked() => OnResumeClicked?.Invoke();
        private void HandleSettingsClicked() => OnSettingsClicked?.Invoke();
        private void HandleMainMenuClicked() => OnMainMenuClicked?.Invoke();

        public override void Dispose()
        {
            if (_resumeButton != null) _resumeButton.clicked -= HandleResumeClicked;
            if (_settingsButton != null) _settingsButton.clicked -= HandleSettingsClicked;
            if (_mainMenuButton != null) _mainMenuButton.clicked -= HandleMainMenuClicked;
            base.Dispose();
        }
    }
}
