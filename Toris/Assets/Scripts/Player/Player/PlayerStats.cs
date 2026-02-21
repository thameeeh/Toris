using OutlandHaven.UIToolkit;
using System;
using UnityEngine;
using static PixelCrushers.AnimatorSaver;

// PURPOSE: Holds the player's health and stamina. Emits events when values change,
// and fires OnPlayerDied when HP reaches zero.

public class PlayerStats : MonoBehaviour
{
    [Header("Health")] public float maxHP = 100f; public float currentHP = 100f;
    [Header("Stamina")] public float maxStamina = 100f; public float currentStamina = 100f; public float staminaRegenPerSec = 10f;

    [Header("Player Data Asset")]
    [SerializeField]
    PlayerDataSO _playerdata;

    public event Action<float, float> OnHealthChanged;
    public event Action<float, float> OnStaminaChanged;
    public event Action OnPlayerDied;

    void Update()
    {
        if (currentStamina < maxStamina)
        {
            currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRegenPerSec * Time.deltaTime);
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);

            _playerdata.OnManaChanged.Invoke(currentStamina, maxStamina);
        }
    }

    public void ApplyDamage(float amount)
    {
        currentHP = Mathf.Max(0, currentHP - amount);
        OnHealthChanged?.Invoke(currentHP, maxHP);

        if (currentHP <= 0) OnPlayerDied?.Invoke();
    }

    public bool TryConsumeStamina(float cost)
    {
        if (currentStamina < cost) return false;
        currentStamina -= cost;
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);

        _playerdata.OnManaChanged.Invoke(currentStamina, maxStamina);

        return true;
    }
}
