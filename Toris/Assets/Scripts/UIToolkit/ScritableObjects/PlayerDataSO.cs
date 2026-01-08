using System;
using UnityEngine;

namespace OutlandHaven.UIToolkit
{

    [CreateAssetMenu(menuName = "UI/Scriptable Objects/PlayerDataSO")]
    public class PlayerDataSO : ScriptableObject
    {
        [Header("Base Stats")]
        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private float _maxMana = 100f;

        [Header("Current State (Runtime)")]
        [SerializeField] private float _currentHealth;
        [SerializeField] private float _currentMana;

        [SerializeField] private int _level = 1;
        [SerializeField] private float _experience = 0;

        [SerializeField] private int _gold = 0;

        // UI and Gameplay systems will listen to these events
        // Passing 'float' allows the UI to know the % (current / max)
        public event Action<float, float> OnHealthChanged; // current, max
        public event Action<float, float> OnManaChanged;   // current, max
        public event Action<int  , float> OnLevelChanged;     // current level, experience
        public event Action<int  , int>   OnGoldChanged;      // current gold, change amount

        public float GetMaxHealth() => _maxHealth;
        public float GetMaxMana() => _maxMana;

        private void OnEnable()
        {
            // Reset state when the game starts (or the SO loads)
            _currentHealth = _maxHealth;
            _currentMana = _maxMana;
        }

        public void ModifyHealth(float amount)
        {
            _currentHealth += amount;

            // Clamp ensures we don't go below 0 or above Max
            _currentHealth = Mathf.Clamp(_currentHealth, 0, _maxHealth);

            // Notify the UI immediately
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);

            if (_currentHealth <= 0)
            {
                Debug.Log("Player has died.");
                // trigger death event here later
            }
        }

        public void ModifyMana(float amount)
        {
            _currentMana += amount;
            _currentMana = Mathf.Clamp(_currentMana, 0, _maxMana);

            OnManaChanged?.Invoke(_currentMana, _maxMana);
        }

        public void AddExperience(int amount)
        {
            _experience += amount;

            _level = (int)(_experience / 100) + 1; // Simple leveling logic

            OnLevelChanged?.Invoke(_level, _experience);
        }

        public void ModifyGold(int amount)
        {
            _gold += amount;
            if (_gold < 0) _gold = 0; // Prevent negative gold
            OnGoldChanged?.Invoke(_gold, amount);
        }

        // Helper for UI initialization
        public float GetHealthPercentage() => _currentHealth / _maxHealth;
        public float GetManaPercentage() => _currentMana / _maxMana;
    }
}
