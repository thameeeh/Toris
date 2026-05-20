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
        private float _lastUpdateTime;
        private IVisualElementScheduledItem _updateTask;

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
                _uiSkillEvents.OnAbilitySlotsUpdated += HandleAbilitySlotsUpdated;
                _eventsBound = true;
            }

            if (_updateTask == null)
            {
                _lastUpdateTime = Time.time;
                _updateTask = _topElement.schedule.Execute(OnUpdate).Every(33); // ~30 FPS is enough for UI
            }
            else
            {
                _lastUpdateTime = Time.time;
                _updateTask.Resume();
            }

            RefreshAll();
        }

        private void OnUpdate()
        {
            float currentTime = Time.time;
            float dt = currentTime - _lastUpdateTime;
            _lastUpdateTime = currentTime;

            foreach (var view in _slotViews)
            {
                view.Tick(dt);
            }
        }

        public void Hide()
        {
            _uiSkillEvents?.OnAbilityTooltipHide?.Invoke();

            if (_eventsBound && _uiSkillEvents != null)
            {
                _uiSkillEvents.OnAbilitySlotPressed -= HandleAbilitySlotPressed;
                _uiSkillEvents.OnAbilitySlotsUpdated -= HandleAbilitySlotsUpdated;
                _eventsBound = false;
            }

            _updateTask?.Pause();
        }

        private void InitializeSlots()
        {
            if (_topElement == null || _abilityController == null) return;

            DisposeSlots();
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

                    var slotView = new AbilitySlotView(slotElement, hotkey.ToUpper(), _uiSkillEvents);
                    _slotViews.Add(slotView);
                }
            }
        }

        public void RefreshAll()
        {
            if (_abilityController == null) return;

            var snapshots = _abilityController.BuildAbilitySlotSnapshots();
            HandleAbilitySlotsUpdated(snapshots);
        }

        private void HandleAbilitySlotPressed(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < _slotViews.Count)
            {
                _slotViews[slotIndex].TriggerPressEffect();
            }
        }

        private void HandleAbilitySlotsUpdated(PlayerAbilitySlotSnapshot[] snapshots)
        {
            if (snapshots == null) return;

            int count = Mathf.Min(_slotViews.Count, snapshots.Length);
            for (int i = 0; i < count; i++)
            {
                _slotViews[i].Update(snapshots[i]);
            }
        }

        public void Dispose()
        {
            Hide();
            DisposeSlots();
        }

        private void DisposeSlots()
        {
            foreach (AbilitySlotView slotView in _slotViews)
            {
                slotView.Dispose();
            }
        }
    }

    public class AbilitySlotView : IDisposable
    {
        private VisualElement _root;
        private Image _icon;
        private VisualElement _cooldownOverlay;
        private Label _timerLabel;
        private Label _hotkeyLabel;
        private Label _manaLabel;
        private UISkillEventsSO _uiSkillEvents;
        private PlayerAbilitySlotSnapshot _snapshot;

        private float _cooldownDuration;
        private float _cooldownRemaining;

        public AbilitySlotView(VisualElement root, string hotkey, UISkillEventsSO uiSkillEvents)
        {
            // If root is already the ability-slot, use it. Otherwise find it.
            _root = root.ClassListContains("ability-slot") ? root : root.Q<VisualElement>(className: "ability-slot");
            _uiSkillEvents = uiSkillEvents;
            
            _icon = root.Q<Image>("ability-icon");
            _cooldownOverlay = root.Q<VisualElement>("cooldown-overlay");
            _timerLabel = root.Q<Label>("cooldown-timer");
            _hotkeyLabel = root.Q<Label>("hotkey-label");
            _manaLabel = root.Q<Label>("mana-cost");

            if (_hotkeyLabel != null) _hotkeyLabel.text = hotkey;

            if (_root != null)
            {
                _root.pickingMode = PickingMode.Position;
                _root.RegisterCallback<PointerEnterEvent>(OnPointerEnter);
                _root.RegisterCallback<PointerMoveEvent>(OnPointerMove);
                _root.RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
            }

            if (_icon != null) _icon.pickingMode = PickingMode.Ignore;
            if (_cooldownOverlay != null) _cooldownOverlay.pickingMode = PickingMode.Ignore;
            if (_timerLabel != null) _timerLabel.pickingMode = PickingMode.Ignore;
            if (_hotkeyLabel != null) _hotkeyLabel.pickingMode = PickingMode.Ignore;
            if (_manaLabel != null) _manaLabel.pickingMode = PickingMode.Ignore;
        }

        public void Update(PlayerAbilitySlotSnapshot snapshot)
        {
            _snapshot = snapshot;

            if (snapshot.HasAbility)
            {
                _icon.style.backgroundImage = new StyleBackground(snapshot.Icon);
                _icon.style.display = DisplayStyle.Flex;

                if (_manaLabel != null)
                {
                    _manaLabel.text = snapshot.ResourceCost.ToString("F0");
                    _manaLabel.style.display = snapshot.ResourceCost > 0 ? DisplayStyle.Flex : DisplayStyle.None;
                    
                    // Visual feedback for affordability
                    _manaLabel.style.color = snapshot.CanAfford ? Color.white : Color.red;
                    _icon.tintColor = snapshot.CanAfford ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.8f);
                }
                
                // If the snapshot says we are on cooldown, but our local timer isn't running,
                // sync it up.
                if (snapshot.IsOnCooldown && _cooldownRemaining <= 0)
                {
                    _cooldownDuration = snapshot.CooldownDuration;
                    _cooldownRemaining = snapshot.CooldownRemaining;
                    _timerLabel.style.display = _cooldownDuration > 1f ? DisplayStyle.Flex : DisplayStyle.None;
                    _root.RemoveFromClassList("ability-slot--ready");
                }
                else if (!snapshot.IsOnCooldown && _cooldownRemaining > 0)
                {
                    // Snap to ready if the backend says so
                    _cooldownRemaining = 0;
                    _cooldownOverlay.style.height = Length.Percent(0);
                    _timerLabel.style.display = DisplayStyle.None;
                    _root.AddToClassList("ability-slot--ready");
                    _root.schedule.Execute(() => _root.RemoveFromClassList("ability-slot--ready")).StartingIn(500);
                }

                // Handle Unlock state (dim if locked)
                _root.style.opacity = snapshot.IsUnlocked ? 1.0f : 0.3f;
            }
            else
            {
                _icon.style.backgroundImage = null;
                _icon.style.display = DisplayStyle.None;
                _cooldownOverlay.style.height = Length.Percent(0);
                _timerLabel.style.display = DisplayStyle.None;
                if (_manaLabel != null) _manaLabel.style.display = DisplayStyle.None;
                _root.style.opacity = 1.0f;
            }
        }

        private void OnPointerEnter(PointerEnterEvent evt)
        {
            if (!_snapshot.HasAbility)
                return;

            _uiSkillEvents?.OnAbilityTooltipShow?.Invoke(_snapshot, evt.position);
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!_snapshot.HasAbility)
                return;

            _uiSkillEvents?.OnAbilityTooltipMove?.Invoke(evt.position);
        }

        private void OnPointerLeave(PointerLeaveEvent evt)
        {
            _uiSkillEvents?.OnAbilityTooltipHide?.Invoke();
        }

        public void TriggerPressEffect()
        {
            _root.AddToClassList("ability-slot--pressed");
            // Remove after a short delay (USS transition handles the rest)
            _root.schedule.Execute(() => _root.RemoveFromClassList("ability-slot--pressed")).StartingIn(100);
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

        public void Tick(float dt)
        {
            if (_cooldownRemaining > 0)
            {
                _cooldownRemaining -= dt;
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

        public void Dispose()
        {
            if (_root == null)
                return;

            _uiSkillEvents?.OnAbilityTooltipHide?.Invoke();
            _root.UnregisterCallback<PointerEnterEvent>(OnPointerEnter);
            _root.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            _root.UnregisterCallback<PointerLeaveEvent>(OnPointerLeave);
        }
    }
}
