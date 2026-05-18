using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections;
using System.Collections.Generic;
using OutlandHaven.Skills;
using UnityEngine.InputSystem;

namespace OutlandHaven.UIToolkit
{
    public class PlayerAbilityHUDView : IDisposable
    {
        private VisualElement _topElement;
        private VisualTreeAsset _slotTemplate;
        private UISkillEventsSO _uiSkillEvents;
        private PlayerAbilityController _abilityController;

        private VisualElement _skillBarContainer;
        private List<AbilitySlotView> _slotViews = new List<AbilitySlotView>();

        private bool _eventsBound = false;

        public PlayerAbilityHUDView(VisualElement topElement, VisualTreeAsset slotTemplate, UISkillEventsSO uiSkillEvents)
        {
            _topElement = topElement;
            _slotTemplate = slotTemplate;
            _uiSkillEvents = uiSkillEvents;

            SetVisualElements();
        }

        private void SetVisualElements()
        {
            _skillBarContainer = _topElement.Q<VisualElement>("hud__skill-bar");
        }

        public void Setup(PlayerAbilityController abilityController)
        {
            _abilityController = abilityController;
            InitializeSlots();
        }

        public void Show()
        {
            if (!_eventsBound && _uiSkillEvents != null)
            {
                _uiSkillEvents.OnAbilitySlotPressed += HandleAbilitySlotPressed;
                _uiSkillEvents.OnAbilityCooldownStarted += HandleCooldownStarted;
                _uiSkillEvents.OnAbilityReady += HandleAbilityReady;
                _eventsBound = true;
            }

            // Start the update loop using schedule
            _topElement.schedule.Execute(OnUpdate).Every(33); // ~30 FPS is enough for UI

            RefreshAll();
        }

        private void OnUpdate()
        {
            foreach (var view in _slotViews)
            {
                view.Tick();
            }
        }

        public void Hide()
        {
            if (_eventsBound && _uiSkillEvents != null)
            {
                _uiSkillEvents.OnAbilitySlotPressed -= HandleAbilitySlotPressed;
                _uiSkillEvents.OnAbilityCooldownStarted -= HandleCooldownStarted;
                _uiSkillEvents.OnAbilityReady -= HandleAbilityReady;
                _eventsBound = false;
            }
        }

        private void InitializeSlots()
        {
            if (_topElement == null || _abilityController == null) return;

            _slotViews.Clear();

            int slotCount = _abilityController.SlotCount;
            
            // Read bindings directly from the generated Input Actions class
            using var tempActions = new InputSystem_Actions();
            UnityEngine.InputSystem.InputAction[] actions = {
                tempActions.Player.Ability1,
                tempActions.Player.Ability2,
                tempActions.Player.Ability3,
                tempActions.Player.Ability4,
                tempActions.Player.Ability5
            };

            for (int i = 0; i < slotCount; i++)
            {
                // 1. Try to find a pre-placed slot created by the UI Designer in UI Builder
                VisualElement slotElement = _topElement.Q<VisualElement>($"hud__ability-slot-{i}");

                // 2. Fallback: If not found, instantiate it dynamically into the default container
                if (slotElement == null && _skillBarContainer != null)
                {
                    TemplateContainer slotInstance = _slotTemplate.Instantiate();
                    slotInstance.name = $"hud__ability-slot-{i}";
                    slotInstance.style.flexGrow = 1;
                    _skillBarContainer.Add(slotInstance);
                    slotElement = slotInstance;
                }

                if (slotElement != null)
                {
                    string hotkey = i < actions.Length ? actions[i].GetBindingDisplayString(0) : "";
                    
                    // Clean up string like "Keyboard/Q" to just "Q"
                    if (hotkey.Contains("/"))
                    {
                        hotkey = hotkey.Substring(hotkey.LastIndexOf('/') + 1);
                    }

                    var slotView = new AbilitySlotView(slotElement, hotkey.ToUpper());
                    _slotViews.Add(slotView);
                }
            }
        }

        public void RefreshAll()
        {
            if (_abilityController == null) return;

            for (int i = 0; i < _slotViews.Count; i++)
            {
                var runtime = _abilityController.GetRuntime(i);
                _slotViews[i].Update(runtime);
            }
        }

        private void HandleAbilitySlotPressed(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < _slotViews.Count)
            {
                _slotViews[slotIndex].TriggerPressEffect();
            }
        }

        private void HandleCooldownStarted(int slotIndex, float duration)
        {
            if (slotIndex >= 0 && slotIndex < _slotViews.Count)
            {
                _slotViews[slotIndex].StartCooldown(duration);
            }
        }

        private void HandleAbilityReady(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < _slotViews.Count)
            {
                _slotViews[slotIndex].SetReady();
            }
        }

        public void Dispose()
        {
            Hide();
        }
    }

    public class AbilitySlotView
    {
        private VisualElement _root;
        private Image _icon;
        private VisualElement _cooldownOverlay;
        private Label _timerLabel;
        private Label _hotkeyLabel;
        private Label _manaLabel;

        private PlayerAbilityRuntime _currentRuntime;
        private float _cooldownDuration;
        private float _cooldownRemaining;

        public AbilitySlotView(VisualElement root, string hotkey)
        {
            // If root is already the ability-slot, use it. Otherwise find it.
            _root = root.ClassListContains("ability-slot") ? root : root.Q<VisualElement>(className: "ability-slot");
            
            _icon = root.Q<Image>("ability-icon");
            _cooldownOverlay = root.Q<VisualElement>("cooldown-overlay");
            _timerLabel = root.Q<Label>("cooldown-timer");
            _hotkeyLabel = root.Q<Label>("hotkey-label");
            _manaLabel = root.Q<Label>("mana-cost");

            if (_hotkeyLabel != null) _hotkeyLabel.text = hotkey;
        }

        public void Update(PlayerAbilityRuntime runtime)
        {
            _currentRuntime = runtime;
            if (runtime != null && runtime.Definition != null)
            {
                _icon.style.backgroundImage = new StyleBackground(runtime.Definition.icon);
                _icon.style.display = DisplayStyle.Flex;

                // TODO: Replace test mana cost (15) with runtime.Definition.manaCost or similar
                // Dependency: Requires manaCost field in PlayerAbilitySO
                if (_manaLabel != null)
                {
                    _manaLabel.text = "15"; 
                    _manaLabel.style.display = DisplayStyle.Flex;
                }
                
                if (runtime.IsOnCooldown)
                {
                    // If we missed the event, we can try to estimate or get it from runtime
                    // For now, let's assume events are reliable.
                }
            }
            else
            {
                _icon.style.backgroundImage = null;
                _icon.style.display = DisplayStyle.None;
                _cooldownOverlay.style.height = Length.Percent(0);
                _timerLabel.style.display = DisplayStyle.None;
                if (_manaLabel != null) _manaLabel.style.display = DisplayStyle.None;
            }
        }

        public void TriggerPressEffect()
        {
            _root.AddToClassList("ability-slot--pressed");
            // Remove after a short delay (USS transition handles the rest)
            // But we need to remove it so it can be triggered again.
            // Using a simple timer or just wait a frame?
            _root.schedule.Execute(() => _root.RemoveFromClassList("ability-slot--pressed")).StartingIn(100);
        }

        public void StartCooldown(float duration)
        {
            _cooldownDuration = duration;
            _cooldownRemaining = duration;
            _timerLabel.style.display = duration > 1f ? DisplayStyle.Flex : DisplayStyle.None;
            _root.RemoveFromClassList("ability-slot--ready");
        }

        public void SetReady()
        {
            _cooldownRemaining = 0;
            _cooldownOverlay.style.height = Length.Percent(0);
            _timerLabel.style.display = DisplayStyle.None;
            _root.AddToClassList("ability-slot--ready");
            
            // Pulse effect?
            _root.schedule.Execute(() => _root.RemoveFromClassList("ability-slot--ready")).StartingIn(500);
        }

        public void Tick()
        {
            if (_cooldownRemaining > 0)
            {
                _cooldownRemaining -= Time.deltaTime;
                float percent = (_cooldownRemaining / _cooldownDuration) * 100f;
                _cooldownOverlay.style.height = Length.Percent(percent);

                if (_cooldownRemaining > 0)
                {
                    _timerLabel.text = _cooldownRemaining.ToString("F1");
                }
                else
                {
                    SetReady();
                }
            }
        }
    }
}
