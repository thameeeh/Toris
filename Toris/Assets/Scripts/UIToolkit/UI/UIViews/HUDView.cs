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
        private ProgressBar _xpBar; // Optional: If you want an XP bar
        private Label _levelLabel;
        private Label _goldLabel;
        private Button _mainToggleBtn;
        private VisualElement _optionsContainer;

        private VisualTreeAsset _buttonTemplate;

        // Data Reference
        private PlayerDataSO _playerData;
        


        // Constructor receives the Data
        public HUDView(VisualElement topElement, PlayerDataSO data, UIEventsSO uiEvents, VisualTreeAsset template) : base(topElement, uiEvents)
        {
            _playerData = data;
            _buttonTemplate = template;

            GenerateMenuButtons();

            if (_playerData != null)
            {
                UpdateHealthUI(_playerData.GetHealthPercentage(), 1f);
                UpdateManaUI(_playerData.GetManaPercentage(), 1f);
                UpdateGoldUI(0, 0);
                UpdateLevelUI(1, 0);
            }
        }

        protected override void SetVisualElements()
        {
            // Player Stats
            _healthBar = m_TopElement.Q<ProgressBar>("hud__health-bar");
            _manaBar = m_TopElement.Q<ProgressBar>("hud__mana-bar");

            _xpBar = m_TopElement.Q<ProgressBar>("hud__xp-bar");
            _levelLabel = m_TopElement.Q<Label>("hud__level-label");
            _goldLabel = m_TopElement.Q<Label>("hud__gold-label");

            // Menu Tab Elements
            _mainToggleBtn = m_TopElement.Q<Button>("hud__menu-tab");
            _optionsContainer = m_TopElement.Q<VisualElement>("hud__menu-options");

            // Clear any placeholder content from the UI Builder
            _optionsContainer?.Clear();
        }

        private void GenerateMenuButtons()
        {
            if (_optionsContainer == null) return;

            CreateMenuButton("Inventory", "(I)", ScreenType.Inventory);
            CreateMenuButton("Skills", "(K)", ScreenType.CharacterSheet);
            // Add other buttons here
        }

        protected override void RegisterButtonCallbacks()
        {
            _mainToggleBtn.RegisterCallback<ClickEvent>(ToggleMenu);
        }

        private void CreateMenuButton(string name, string shortcut, ScreenType targetScreen)
        {
            if (_buttonTemplate == null)
            {
                Debug.LogError("Template not loaded! Check Resources path.");
                return;
            }

            // 1. Instantiate the template
            TemplateContainer instance = _buttonTemplate.Instantiate();

            // 2. Setup the Data (Find elements INSIDE the new instance)
            var btnRoot = instance.Q<Button>("menu-btn-root");
            var label = instance.Q<Label>("menu-btn-label");
            var shortcutLabel = instance.Q<Label>("menu-btn-shortcut");

            label.text = name;
            shortcutLabel.text = shortcut;

            // 3. Register Click Event
            btnRoot.RegisterCallback<ClickEvent>(evt =>
            {
                // Close the mini-menu
                ToggleMenu(null);

                // Open the target window
                UIEvents.OnRequestOpen?.Invoke(targetScreen, null);
            });

            // 4. Add to the container
            _optionsContainer.Add(instance);
        }

        private void ToggleMenu(ClickEvent evt)
        {
            // Toggle logic: check if display is None, switch to Flex, etc.
            bool isHidden = _optionsContainer.style.display == DisplayStyle.None;
            _optionsContainer.style.display = isHidden ? DisplayStyle.Flex : DisplayStyle.None;
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
            // Unity Progress Bar is 0-100
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
                // Simple example: 0 to 100 XP per level
                // In a real game, you'd calculate % of current level cap
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