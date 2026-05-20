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
        private Label _statAttackSpeed;
        private Label _statDamageReduction;
        private Label _statMagicDefense;
        private Label _statStaminaRegen;

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
            _statAttackSpeed = _topElement.Q<Label>("stat-attackSpeed");
            _statDamageReduction = _topElement.Q<Label>("stat-damageReduction");
            _statMagicDefense = _topElement.Q<Label>("stat-magicDefense");
            _statStaminaRegen = _topElement.Q<Label>("stat-staminaRegen");
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
                _statMoveSpeed.text = $"Movement Speed: {effects.moveSpeedMultiplier:F2}x";

            if (_statOutgoingDamage != null)
            {
                float weaponDamage = _hudBridge != null ? _hudBridge.CurrentWeaponDamage : 0f;
                _statOutgoingDamage.text = weaponDamage > 0f
                    ? $"Damage: {weaponDamage:F0} ({effects.outgoingDamageMultiplier:F2}x)"
                    : $"Damage: {effects.outgoingDamageMultiplier:F2}x";
            }

            if (_statAttackSpeed != null)
            {
                float attackSpeed = _hudBridge != null ? _hudBridge.CurrentWeaponAttackSpeed : 0f;
                _statAttackSpeed.text = attackSpeed > 0f
                    ? $"Attack Speed: {attackSpeed:F2}/s"
                    : "Attack Speed: -";
            }

            if (_statDamageReduction != null)
            {
                float damageReductionPercent = (1f - effects.incomingDamageMultiplier) * 100f;
                _statDamageReduction.text = $"Damage Reduction: {damageReductionPercent:+0.#;-0.#;0}%";
            }

            if (_statMagicDefense != null)
            {
                float magicDefense = _hudBridge != null ? _hudBridge.CurrentMagicDefense : 0f;
                _statMagicDefense.text = $"Magic Defense: {magicDefense:F0}";
            }

            if (_statStaminaRegen != null)
                _statStaminaRegen.text = $"Stamina Regen: {effects.staminaRegenPerSecond:F1}/s";
        }

        public void Dispose()
        {
            Hide();
        }
    }
}
