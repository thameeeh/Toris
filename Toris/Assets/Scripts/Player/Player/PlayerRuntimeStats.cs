using System;
using UnityEngine;

[Serializable]
public class PlayerRuntimeStats
{
    [SerializeField] private float _currentHealth;
    [SerializeField] private float _currentStamina;

    public float CurrentHealth => _currentHealth;
    public float CurrentStamina => _currentStamina;

    public void Initialize(float maxHealth, float maxStamina, bool fillToMaximum)
    {
        if (fillToMaximum)
        {
            _currentHealth = Mathf.Max(0f, maxHealth);
            _currentStamina = Mathf.Max(0f, maxStamina);
        }
        else
        {
            _currentHealth = Mathf.Clamp(_currentHealth, 0f, Mathf.Max(0f, maxHealth));
            _currentStamina = Mathf.Clamp(_currentStamina, 0f, Mathf.Max(0f, maxStamina));
        }
    }

    public void ClampToMaximums(float maxHealth, float maxStamina)
    {
        _currentHealth = Mathf.Clamp(_currentHealth, 0f, Mathf.Max(0f, maxHealth));
        _currentStamina = Mathf.Clamp(_currentStamina, 0f, Mathf.Max(0f, maxStamina));
    }

    public void SetCurrentHealth(float value, float maxHealth)
    {
        _currentHealth = Mathf.Clamp(value, 0f, Mathf.Max(0f, maxHealth));
    }

    public void SetCurrentStamina(float value, float maxStamina)
    {
        _currentStamina = Mathf.Clamp(value, 0f, Mathf.Max(0f, maxStamina));
    }

    public void ApplyDamage(float amount, float maxHealth)
    {
        float validatedAmount = Mathf.Max(0f, amount);
        _currentHealth = Mathf.Clamp(_currentHealth - validatedAmount, 0f, Mathf.Max(0f, maxHealth));
    }

    public void RestoreHealth(float amount, float maxHealth)
    {
        float validatedAmount = Mathf.Max(0f, amount);
        _currentHealth = Mathf.Clamp(_currentHealth + validatedAmount, 0f, Mathf.Max(0f, maxHealth));
    }

    public bool TryConsumeStamina(float amount)
    {
        float validatedAmount = Mathf.Max(0f, amount);

        if (_currentStamina < validatedAmount)
            return false;

        _currentStamina -= validatedAmount;
        return true;
    }

    public void RestoreStamina(float amount, float maxStamina)
    {
        float validatedAmount = Mathf.Max(0f, amount);
        _currentStamina = Mathf.Clamp(_currentStamina + validatedAmount, 0f, Mathf.Max(0f, maxStamina));
    }

    public bool IsDead()
    {
        return _currentHealth <= 0f;
    }
}