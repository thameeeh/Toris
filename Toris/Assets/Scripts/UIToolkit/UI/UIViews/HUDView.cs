using UnityEngine.UIElements;
using UnityEngine;

namespace OutlandHaven.UIToolkit
{
    public class HUDView : GameView
    {
        public override ScreenType ID => ScreenType.HUD;

        // Visual Elements
        private ProgressBar _healthBar;
        private ProgressBar _manaBar;
        private ProgressBar _xpBar;
        private Label _levelLabel;
        private Label _goldLabel;

        // Data Reference
        private PlayerDataSO _playerData;

        public HUDView(VisualElement topElement, PlayerDataSO data) : base(topElement)
        {
            _playerData = data;

            UpdateHealthUI(_playerData.GetHealthPercentage(), 1f);
            UpdateManaUI(_playerData.GetManaPercentage(), 1f);

            UpdateLevelUI(0, 0); // Placeholder
        }

        protected override void SetVisualElements()
        {
            // These names must match your UXML 'name' attributes exactly
            _healthBar = m_TopElement.Q<ProgressBar>("hud__health-bar");
            _manaBar = m_TopElement.Q<ProgressBar>("hud__mana-bar");

            // New elements for the new Data
            _xpBar = m_TopElement.Q<ProgressBar>("hud__xp-bar");
            _levelLabel = m_TopElement.Q<Label>("hud__level-label");
            _goldLabel = m_TopElement.Q<Label>("hud__gold-label");
        }

        public override void Show()
        {
            base.Show();
            // Subscribe to ALL events
            if (_playerData != null)
            {
                _playerData.OnHealthChanged += UpdateHealthUI;
                _playerData.OnManaChanged += UpdateManaUI;
                _playerData.OnLevelChanged += UpdateLevelUI;
                _playerData.OnGoldChanged += UpdateGoldUI;

                _playerData.AddExperience(0); // Trigger initial level/XP update
                _playerData.ModifyGold(0);    // Trigger initial gold update
            }
        }

        public override void Hide()
        {
            base.Hide();
            // Unsubscribe from ALL events
            if (_playerData != null)
            {
                _playerData.OnHealthChanged -= UpdateHealthUI;
                _playerData.OnManaChanged -= UpdateManaUI;
                _playerData.OnLevelChanged -= UpdateLevelUI;
                _playerData.OnGoldChanged -= UpdateGoldUI;
            }
        }

        // --- Event Handlers ---

        private void UpdateHealthUI(float current, float max)
        {
            if (_healthBar == null) return;
            //Progress Bar is 0-100
            _healthBar.value = (current / max) * 100f;
        }

        private void UpdateManaUI(float current, float max)
        {
            if (_manaBar == null) return;
            _manaBar.value = (current / max) * 100f;
        }

        private void UpdateLevelUI(int level, float experience)
        {
            if (_levelLabel != null)
                _levelLabel.text = $"Level {level}";

            if (_xpBar != null)
            {
                float xpPercent = experience % 100;
                _xpBar.value = xpPercent;
            }
        }

        private void UpdateGoldUI(int currentGold, int changeAmount)
        {
            if (_goldLabel != null)
                _goldLabel.text = $"Gold: {currentGold}";

            // Optional: You could spawn a "floating text" effect here using 'changeAmount'
            // e.g. if changeAmount > 0, show green "+50" text.
        }
    }
}