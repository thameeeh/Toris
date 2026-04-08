using System;
using UnityEngine;

// PURPOSE:
// - UI-facing bridge for player runtime data
// - Aggregates gameplay-owned player data into one presentation-friendly surface
// - Keeps UI from wiring directly into multiple gameplay systems

public class PlayerHUDBridge : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerStats _playerStats;
    [SerializeField] private PlayerProgression _playerProgression;
    [SerializeField] private PlayerStatusController _playerStatusController;

    public event Action<float, float> OnHealthChanged;
    public event Action<float, float> OnStaminaChanged;
    public event Action<int, float> OnLevelChanged;
    public event Action<int, int> OnGoldChanged;

    public event Action<PlayerStatusEffectType> OnStatusApplied;
    public event Action<PlayerStatusEffectType> OnStatusRemoved;
    public event Action<PlayerStatusEffectType, float> OnStatusDamageTick;

    public float CurrentHealth => _playerStats != null ? _playerStats.currentHP : 0f;
    public float MaxHealth => _playerStats != null ? _playerStats.maxHP : 0f;

    public float CurrentStamina => _playerStats != null ? _playerStats.currentStamina : 0f;
    public float MaxStamina => _playerStats != null ? _playerStats.maxStamina : 0f;

    public int CurrentLevel => _playerProgression != null ? _playerProgression.CurrentLevel : 1;
    public float CurrentExperience => _playerProgression != null ? _playerProgression.CurrentExperience : 0f;
    public int CurrentGold => _playerProgression != null ? _playerProgression.CurrentGold : 0;

    public float ExperienceProgressNormalized =>
        _playerProgression != null ? _playerProgression.GetExperienceProgressNormalized() : 0f;

    private void OnValidate()
    {
        if (_playerStats == null)
        {
            Debug.LogWarning($"[PlayerHUDBridge] Missing PlayerStats on {name}", this);
        }

        if (_playerProgression == null)
        {
            Debug.LogWarning($"[PlayerHUDBridge] Missing PlayerProgression on {name}", this);
        }

        if (_playerStatusController == null)
        {
            Debug.LogWarning($"[PlayerHUDBridge] Missing PlayerStatusController on {name}", this);
        }
    }

    private void OnEnable()
    {
        if (_playerStats != null)
        {
            _playerStats.OnHealthChanged += HandleHealthChanged;
            _playerStats.OnStaminaChanged += HandleStaminaChanged;
        }

        if (_playerProgression != null)
        {
            _playerProgression.OnLevelChanged += HandleLevelChanged;
            _playerProgression.OnGoldChanged += HandleGoldChanged;
        }

        if (_playerStatusController != null)
        {
            _playerStatusController.OnStatusApplied += HandleStatusApplied;
            _playerStatusController.OnStatusRemoved += HandleStatusRemoved;
            _playerStatusController.OnStatusDamageTick += HandleStatusDamageTick;
        }
    }

    private void OnDisable()
    {
        if (_playerStats != null)
        {
            _playerStats.OnHealthChanged -= HandleHealthChanged;
            _playerStats.OnStaminaChanged -= HandleStaminaChanged;
        }

        if (_playerProgression != null)
        {
            _playerProgression.OnLevelChanged -= HandleLevelChanged;
            _playerProgression.OnGoldChanged -= HandleGoldChanged;
        }

        if (_playerStatusController != null)
        {
            _playerStatusController.OnStatusApplied -= HandleStatusApplied;
            _playerStatusController.OnStatusRemoved -= HandleStatusRemoved;
            _playerStatusController.OnStatusDamageTick -= HandleStatusDamageTick;
        }
    }

    private void Start()
    {
        PushInitialState();
    }

    public void PushInitialState()
    {
        if (_playerStats != null)
        {
            OnHealthChanged?.Invoke(_playerStats.currentHP, _playerStats.maxHP);
            OnStaminaChanged?.Invoke(_playerStats.currentStamina, _playerStats.maxStamina);
        }

        if (_playerProgression != null)
        {
            OnLevelChanged?.Invoke(_playerProgression.CurrentLevel, _playerProgression.CurrentExperience);
            OnGoldChanged?.Invoke(_playerProgression.CurrentGold, 0);
        }
    }

    private void HandleHealthChanged(float current, float max)
    {
        OnHealthChanged?.Invoke(current, max);
    }

    private void HandleStaminaChanged(float current, float max)
    {
        OnStaminaChanged?.Invoke(current, max);
    }

    private void HandleLevelChanged(int level, float experience)
    {
        OnLevelChanged?.Invoke(level, experience);
    }

    private void HandleGoldChanged(int currentGold, int delta)
    {
        OnGoldChanged?.Invoke(currentGold, delta);
    }

    private void HandleStatusApplied(PlayerStatusEffectType statusType)
    {
        OnStatusApplied?.Invoke(statusType);
    }

    private void HandleStatusRemoved(PlayerStatusEffectType statusType)
    {
        OnStatusRemoved?.Invoke(statusType);
    }

    private void HandleStatusDamageTick(PlayerStatusEffectType statusType, float damage)
    {
        OnStatusDamageTick?.Invoke(statusType, damage);
    }
}
