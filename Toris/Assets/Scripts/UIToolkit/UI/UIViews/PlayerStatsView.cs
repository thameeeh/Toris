using UnityEngine;
using UnityEngine.UIElements;
using System;

namespace OutlandHaven.Inventory
{
    public class PlayerStatsView : IDisposable
    {
        private VisualElement _topElement;
        private PlayerHUDBridge _hudBridge;

        private Label _statMaxHealth;
        private Label _statMaxStamina;
        private Label _statMoveSpeed;
        private Label _statOutgoingDamage;

        private bool _eventsBound = false;

        public PlayerStatsView(VisualElement topElement)
        {
            _topElement = topElement;
            SetVisualElements();
        }

        private void SetVisualElements()
        {
            _statMaxHealth = _topElement.Q<Label>("stat-maxHealth");
            _statMaxStamina = _topElement.Q<Label>("stat-maxStamina");
            _statMoveSpeed = _topElement.Q<Label>("stat-moveSpeed");
            _statOutgoingDamage = _topElement.Q<Label>("stat-outgoingDamage");
        }

        public void Initialize()
        {
            // Usually anything initial related goes here
        }

        public void Setup(PlayerHUDBridge hudBridge)
        {
            _hudBridge = hudBridge;
            RefreshStats();
        }

        public void Show()
        {
            if (!_eventsBound && _hudBridge != null)
            {
                _hudBridge.OnResolvedEffectsChanged += HandleResolvedEffectsChanged;
                _eventsBound = true;
            }
            RefreshStats();
        }

        public void Hide()
        {
            if (_eventsBound && _hudBridge != null)
            {
                _hudBridge.OnResolvedEffectsChanged -= HandleResolvedEffectsChanged;
                _eventsBound = false;
            }
        }

        private void HandleResolvedEffectsChanged(PlayerResolvedEffects effects)
        {
            UpdateLabels(effects);
        }

        private void RefreshStats()
        {
            if (_hudBridge == null) return;

            UpdateLabels(_hudBridge.ResolvedEffects);
        }

        private void UpdateLabels(PlayerResolvedEffects effects)
        {
            if (_statMaxHealth != null)
                _statMaxHealth.text = $"Max Health: {effects.maxHealth:F0}";

            if (_statMaxStamina != null)
                _statMaxStamina.text = $"Max Stamina: {effects.maxStamina:F0}";

            if (_statMoveSpeed != null)
                _statMoveSpeed.text = $"Move Speed: {effects.moveSpeedMultiplier:F2}x";

            if (_statOutgoingDamage != null)
                _statOutgoingDamage.text = $"Damage: {effects.outgoingDamageMultiplier:F2}x";
        }

        public void Dispose()
        {
            Hide();
        }
    }
}
