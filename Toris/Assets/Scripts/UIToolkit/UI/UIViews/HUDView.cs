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
        private Button _mainToggleBtn;
        private VisualElement _optionsContainer;

        private VisualTreeAsset _buttonTemplate;

        // Data Reference
        private PlayerHUDBridge _playerHudBridge;
        
        // Progress Bar is 0-100
        private const float PROGRESS_BAR_MAX = 100f;

        private bool _isSetup = false;

        // Constructor receives the Data
        public HUDView(VisualElement topElement, PlayerHUDBridge data, UIEventsSO uiEvents, VisualTreeAsset buttonTemplate) : base(topElement, uiEvents)
        {
            _playerHudBridge = data;
            _buttonTemplate = buttonTemplate;
        }

        public override void Setup(object payload)
        {
            if (!_isSetup)
            {
                GenerateMenuButtons();

                if (_playerHudBridge != null)
                {
                    UpdateHealthUI(_playerHudBridge.CurrentHealth, _playerHudBridge.MaxHealth);
                    UpdateManaUI(_playerHudBridge.CurrentStamina, _playerHudBridge.MaxStamina);
                    UpdateGoldUI(_playerHudBridge.CurrentGold, 0);
                    UpdateLevelUI(_playerHudBridge.CurrentLevel, _playerHudBridge.CurrentExperience);
                }
                else 
                {
                    Debug.LogError("HUDView: PlayerHUDBridge data reference is null! HUD will not display player info.");
                }
                _isSetup = true;
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
            _optionsContainer.style.display = DisplayStyle.None; // Start hidden
        }

        private void GenerateMenuButtons()
        {
            if (_optionsContainer == null) return;

            CreateMenuButton("Inventory", "(I)", ScreenType.Inventory);
            CreateMenuButton("Skills", "(K)", ScreenType.CharacterSheet);
            CreateMenuButton("Shop", "(T)", ScreenType.CharacterSheet);
            CreateMenuButton("Map", "(U)", ScreenType.CharacterSheet);
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
            if (_playerHudBridge != null)
            {
                _playerHudBridge.OnHealthChanged += UpdateHealthUI;
                _playerHudBridge.OnStaminaChanged += UpdateManaUI;
                _playerHudBridge.OnLevelChanged += UpdateLevelUI;
                _playerHudBridge.OnGoldChanged += UpdateGoldUI;

                _playerHudBridge.PushInitialState(); // Trigger initial level/XP update
            }
        }

        public override void Hide()
        {
            base.Hide();
            // Unsubscribe from ALL events
            if (_playerHudBridge != null)
            {
                _playerHudBridge.OnHealthChanged -= UpdateHealthUI;
                _playerHudBridge.OnStaminaChanged -= UpdateManaUI;
                _playerHudBridge.OnLevelChanged -= UpdateLevelUI;
                _playerHudBridge.OnGoldChanged -= UpdateGoldUI;
            }
        }

        // --- Event Handlers ---

        private void UpdateHealthUI(float current, float max)
        {
            if (_healthBar == null) return;

            _healthBar.value = (current / max) * PROGRESS_BAR_MAX;
        }

        private void UpdateManaUI(float current, float max)
        {
            if (_manaBar == null) return;
            _manaBar.value = (current / max) * PROGRESS_BAR_MAX;
        }

        private void UpdateLevelUI(int level, float experience)
        {
            if (_levelLabel != null)
                _levelLabel.text = $"Level {level}";

            if (_xpBar != null)
            {
                if (_playerHudBridge != null)
                {
                    _xpBar.value = _playerHudBridge.ExperienceProgressNormalized * PROGRESS_BAR_MAX;
                }
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