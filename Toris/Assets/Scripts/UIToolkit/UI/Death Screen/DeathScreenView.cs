using System;
using UnityEngine.UIElements;

namespace OutlandHaven.UIToolkit
{
    // Dumb MVP view for the death screen. It only exposes button intents.
    public sealed class DeathScreenView : GameView
    {
        public override ScreenType ID => ScreenType.DeathScreen;

        private Button _respawnButton;
        private Button _mainMenuButton;
        private bool _eventsBound;

        public event Action OnRespawnClicked;
        public event Action OnMainMenuClicked;

        public DeathScreenView(VisualElement topElement, UIEventsSO uiEvents) : base(topElement, uiEvents)
        {
        }

        protected override void SetVisualElements()
        {
            _respawnButton = Root.Q<Button>("Btn_Respawn");
            _mainMenuButton = Root.Q<Button>("Btn_MainMenu");
        }

        public override void Show()
        {
            base.Show();
            BindEvents();
            _respawnButton?.Focus();
        }

        public override void Hide()
        {
            UnbindEvents();
            base.Hide();
        }

        public override void Dispose()
        {
            UnbindEvents();
            base.Dispose();
        }

        private void BindEvents()
        {
            if (_eventsBound)
                return;

            if (_respawnButton != null)
                _respawnButton.clicked += HandleRespawnClicked;

            if (_mainMenuButton != null)
                _mainMenuButton.clicked += HandleMainMenuClicked;

            _eventsBound = true;
        }

        private void UnbindEvents()
        {
            if (!_eventsBound)
                return;

            if (_respawnButton != null)
                _respawnButton.clicked -= HandleRespawnClicked;

            if (_mainMenuButton != null)
                _mainMenuButton.clicked -= HandleMainMenuClicked;

            _eventsBound = false;
        }

        private void HandleRespawnClicked() => OnRespawnClicked?.Invoke();

        private void HandleMainMenuClicked() => OnMainMenuClicked?.Invoke();
    }
}
