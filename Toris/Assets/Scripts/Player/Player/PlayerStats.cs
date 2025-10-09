using UnityEngine;
using System;

// PURPOSE: Holds the player's health and stamina. Emits events when values change,
// and fires OnPlayerDied when HP reaches zero.

public class PlayerStats : MonoBehaviour
{
    [Header("Health")] public float maxHP = 100f; public float currentHP = 100f;
    [Header("Stamina")] public float maxStamina = 100f, currentStamina = 100f, staminaRegenPerSec = 10f;

    public event Action<float, float> OnHealthChanged;
    public event Action<float, float> OnStaminaChanged;
    public event Action OnPlayerDied;

    void Update()
    {
        // Passive stamina regen (clamped to max) for now useless, possibly to be removed in the future
        if (currentStamina < maxStamina)
        {
            currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRegenPerSec * Time.deltaTime);
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        }
    }

    public void ApplyDamage(float amount)
    {
        // Reduce HP and notify listeners
        currentHP = Mathf.Max(0, currentHP - amount);
        OnHealthChanged?.Invoke(currentHP, maxHP);

        // If depleted, emit death event (consumers: LifeGate, UI, etc.)
        if (currentHP <= 0) OnPlayerDied?.Invoke();
    }

    public bool TryConsumeStamina(float cost)
    {
        // Spend stamina if available; notify listeners on change
        if (currentStamina < cost) return false;
        currentStamina -= cost;
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        return true;
    }
}
