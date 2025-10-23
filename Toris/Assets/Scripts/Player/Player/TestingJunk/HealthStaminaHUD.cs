using UnityEngine;
using UnityEngine.UI;

public class HealthStaminaHUD : MonoBehaviour
{
    [Header("Refs")]
    public PlayerStats player;
    public Image healthBar;
    public Image staminaBar;

    void Awake()
    {
        if (player == null) player = FindFirstObjectByType<PlayerStats>();
    }

    void OnEnable()
    {
        if (player == null) return;
        player.OnHealthChanged += HandleHealth;
        player.OnStaminaChanged += HandleStamina;

        HandleHealth(player.currentHP, player.maxHP);
        HandleStamina(player.currentStamina, player.maxStamina);
    }

    void OnDisable()
    {
        if (player == null) return;
        player.OnHealthChanged -= HandleHealth;
        player.OnStaminaChanged -= HandleStamina;
    }

    void HandleHealth(float current, float max)
    {
        healthBar.fillAmount = (max <= 0f) ? 0f : Mathf.Clamp01(current / max);
    }

    void HandleStamina(float current, float max)
    {
        staminaBar.fillAmount = (max <= 0f) ? 0f : Mathf.Clamp01(current / max);
    }
}
