using UnityEngine.UIElements;
using OutlandHaven.Inventory;
using OutlandHaven.Tutorial;
using UnityEngine;
using System;

namespace OutlandHaven.UIToolkit
{
    public class HUDView : GameView
    {
        private const string ProgressionTutorialAnchorId = "hud.progression";
        private const string MenuToggleTutorialAnchorId = "hud.menu_toggle";
        private const string InventoryMenuTutorialAnchorId = "hud.inventory_button";

        public override ScreenType ID => ScreenType.HUD;

        // Visual Elements
        private ProgressBar _healthBar;
        private ProgressBar _manaBar;
        private ProgressBar _xpBar;
        private Label _levelLabel;
        private Label _goldLabel;
        private Label _fpsLabel;
        private VisualElement _progressionContainer;
        private Button _mainToggleBtn;
        private Button _inventoryMenuButton;
        private VisualElement _optionsContainer;

        private VisualTreeAsset _buttonTemplate;

        // Data Reference
        private PlayerHUDBridge _playerHudBridge;
        private PlayerSkillTracker _skillTracker;
        private PlayerPotionHUDView _potionHUDView;
        private PlayerAbilityHUDView _abilityHUDView;
        private VisualTreeAsset _slotTemplate;
        private VisualTreeAsset _abilitySlotTemplate;

        // Visual Elements
        private VisualElement _spNotification;
        private Label _spCountLabel;
        
        // Progress Bar is 0-100
        private const float PROGRESS_BAR_MAX = 100f;

        private bool _isSetup = false;

        // Constructor receives the Data
        public HUDView(
            VisualElement topElement, 
            PlayerHUDBridge data, 
            PlayerSkillTracker skillTracker,
            UIEventsSO uiEvents, 
            UIInventoryEventsSO uiInventoryEvents, 
            OutlandHaven.Skills.UISkillEventsSO uiSkillEvents, 
            VisualTreeAsset buttonTemplate, 
            VisualTreeAsset slotTemplate, 
            VisualTreeAsset abilitySlotTemplate) 
            : base(topElement, uiEvents)
        {
            _playerHudBridge = data;
            _skillTracker = skillTracker;
            _buttonTemplate = buttonTemplate;
            _slotTemplate = slotTemplate;
            _abilitySlotTemplate = abilitySlotTemplate;

            _potionHUDView = new PlayerPotionHUDView(topElement, _slotTemplate, uiInventoryEvents); 
            _abilityHUDView = new PlayerAbilityHUDView(topElement, _abilitySlotTemplate, uiSkillEvents);
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
#if UNITY_EDITOR
                    Debug.LogError("HUDView: PlayerHUDBridge data reference is null! HUD will not display player info.");
#endif
                }
                _isSetup = true;
            }

            // Handling multiple possible payloads
            if (payload is InventoryManager potionInventory)
            {
                _potionHUDView?.Setup(potionInventory);
            }
            else if (payload is PlayerAbilityController abilityController)
            {
                _abilityHUDView?.Setup(abilityController);
            }
            else if (payload is ValueTuple<InventoryManager, PlayerAbilityController> tuple)
            {
                _potionHUDView?.Setup(tuple.Item1);
                _abilityHUDView?.Setup(tuple.Item2);
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
            _fpsLabel = m_TopElement.Q<Label>("hud__fps-label");
            _progressionContainer = m_TopElement.Q<VisualElement>("hud__progression-container");

            // SP Notification Element
            _spNotification = m_TopElement.Q<VisualElement>("hud__sp-notification");
            _spCountLabel = m_TopElement.Q<Label>("hud__sp-count");

            // Menu Tab Elements
            _mainToggleBtn = m_TopElement.Q<Button>("hud__menu-tab");
            _optionsContainer = m_TopElement.Q<VisualElement>("hud__menu-options");

            // Clear any placeholder content from the UI Builder
            _optionsContainer?.Clear();
            _optionsContainer.style.display = DisplayStyle.None; // Start hidden
            SetFpsVisible(false);
        }

        public void SetFpsVisible(bool visible)
        {
            if (_fpsLabel == null)
            {
                return;
            }

            _fpsLabel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void SetFpsText(string fpsText)
        {
            if (_fpsLabel == null)
            {
                return;
            }

            _fpsLabel.text = string.IsNullOrWhiteSpace(fpsText) ? "FPS: --" : fpsText;
        }

        private void GenerateMenuButtons()
        {
            if (_optionsContainer == null) return;

            _inventoryMenuButton = CreateMenuButton("Inventory", "(I)", ScreenType.Inventory);
            TutorialAnchorRegistry.Register(InventoryMenuTutorialAnchorId, _inventoryMenuButton);
            CreateMenuButton("Skills", "(U)", ScreenType.Skills);
            CreateMenuButton("Pause", "(ESC)", ScreenType.PauseMenu);
            // Add other buttons here
        }

        protected override void RegisterButtonCallbacks()
        {
            _mainToggleBtn.RegisterCallback<ClickEvent>(ToggleMenu);
            _spNotification?.RegisterCallback<ClickEvent>(evt => 
            {
                UIEvents.OnRequestOpen?.Invoke(ScreenType.Skills, null);
            });
            TutorialAnchorRegistry.Register(ProgressionTutorialAnchorId, _progressionContainer);
            TutorialAnchorRegistry.Register(MenuToggleTutorialAnchorId, _mainToggleBtn);
        }

        private Button CreateMenuButton(string name, string shortcut, ScreenType targetScreen)
        {
            if (_buttonTemplate == null)
            {
#if UNITY_EDITOR
                Debug.LogError("Template not loaded! Check Resources path.");
#endif
                return null;
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
            return btnRoot;
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
            _potionHUDView?.Show();
            _abilityHUDView?.Show();
            
            // Subscribe to ALL events
            if (_playerHudBridge != null)
            {
                _playerHudBridge.OnHealthChanged += UpdateHealthUI;
                _playerHudBridge.OnStaminaChanged += UpdateManaUI;
                _playerHudBridge.OnLevelChanged += UpdateLevelUI;
                _playerHudBridge.OnExperienceChanged += UpdateExperienceUI;
                _playerHudBridge.OnGoldChanged += UpdateGoldUI;

                _playerHudBridge.PushInitialState(); // Trigger initial level/XP update
            }

            if (_skillTracker != null)
            {
                _skillTracker.OnAvailableSPChanged += UpdateSPNotification;
                UpdateSPNotification(_skillTracker.AvailableSP);
            }
        }

        public override void Hide()
        {
            base.Hide();
            _potionHUDView?.Hide();
            _abilityHUDView?.Hide();

            // Unsubscribe from ALL events
            if (_playerHudBridge != null)
            {
                _playerHudBridge.OnHealthChanged -= UpdateHealthUI;
                _playerHudBridge.OnStaminaChanged -= UpdateManaUI;
                _playerHudBridge.OnLevelChanged -= UpdateLevelUI;
                _playerHudBridge.OnExperienceChanged -= UpdateExperienceUI;
                _playerHudBridge.OnGoldChanged -= UpdateGoldUI;
            }

            if (_skillTracker != null)
            {
                _skillTracker.OnAvailableSPChanged -= UpdateSPNotification;
            }
        }

        public override void Dispose()
        {
            TutorialAnchorRegistry.Unregister(ProgressionTutorialAnchorId, _progressionContainer);
            TutorialAnchorRegistry.Unregister(MenuToggleTutorialAnchorId, _mainToggleBtn);
            TutorialAnchorRegistry.Unregister(InventoryMenuTutorialAnchorId, _inventoryMenuButton);
            base.Dispose();
            _potionHUDView?.Dispose();
            _abilityHUDView?.Dispose();
        }

        // --- Event Handlers ---

        private void UpdateSPNotification(int availableSP)
        {
            if (_spNotification == null) return;
            
            if (availableSP > 0)
            {
                _spNotification.style.display = DisplayStyle.Flex;
                if (_spCountLabel != null)
                    _spCountLabel.text = availableSP.ToString();
            }
            else
            {
                _spNotification.style.display = DisplayStyle.None;
            }
        }

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

            UpdateExperienceBar();
        }

        private void UpdateExperienceUI(float experience, int level)
        {
            UpdateExperienceBar();
        }

        private void UpdateExperienceBar()
        {
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
