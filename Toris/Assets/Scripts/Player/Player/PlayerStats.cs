using OutlandHaven.UIToolkit;
using System;
using UnityEngine;

// PURPOSE:
// - PlayerEffectSourceController = owns resolved player gameplay effects
// - PlayerRuntimeStats = owns mutable current HP / stamina
// - PlayerStats = resource bridge for health/stamina + regen + UI compatibility

public class PlayerStats : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerEffectSourceController _effectSourceController;

    [Header("Optional UI Bridge")]
    [SerializeField] private PlayerDataSO _playerData;

    [Header("Runtime")]
    [SerializeField] private bool _fillResourcesToMaximumOnAwake = true;
    [SerializeField] private bool _preserveResourceRatiosWhenEffectsChange = true;

    private PlayerRuntimeStats _runtimeStats;
    private PlayerResolvedEffects _resolvedEffects;

    public event Action<float, float> OnHealthChanged;
    public event Action<float, float> OnStaminaChanged;
    public event Action OnPlayerDied;
    public event Action<PlayerResolvedEffects> OnResolvedEffectsChanged;

    private bool _isDead;
    public bool IsDead => _isDead;
    public PlayerRuntimeStats RuntimeStats => _runtimeStats;
    public PlayerResolvedEffects ResolvedEffects => _resolvedEffects;

    public float maxHP => _resolvedEffects.maxHealth;
    public float currentHP => _runtimeStats != null ? _runtimeStats.CurrentHealth : _resolvedEffects.maxHealth;

    public float maxStamina => _resolvedEffects.maxStamina;
    public float currentStamina => _runtimeStats != null ? _runtimeStats.CurrentStamina : _resolvedEffects.maxStamina;

    public float staminaRegenPerSec => _resolvedEffects.staminaRegenPerSecond;

    private void Awake()
    {
        _runtimeStats = new PlayerRuntimeStats();

        if (_effectSourceController != null)
        {
            _resolvedEffects = _effectSourceController.ResolvedEffects;
        }
        else
        {
            _resolvedEffects = PlayerResolvedEffects.CreateDefault();
        }

        _runtimeStats.Initialize(
            _resolvedEffects.maxHealth,
            _resolvedEffects.maxStamina,
            _fillResourcesToMaximumOnAwake);

        BroadcastAll();
    }

    private void OnEnable()
    {
        if (_effectSourceController != null)
        {
            _effectSourceController.OnResolvedEffectsChanged += HandleResolvedEffectsChanged;
        }
    }

    private void OnDisable()
    {
        if (_effectSourceController != null)
        {
            _effectSourceController.OnResolvedEffectsChanged -= HandleResolvedEffectsChanged;
        }
    }

    private void Update()
    {
        RegenerateStamina(Time.deltaTime);
    }

    private void OnValidate()
    {
        if (_effectSourceController == null)
        {
            Debug.LogWarning($"[PlayerStats] Missing PlayerEffectSourceController on {name}. Defaults will be used.", this);
        }
    }

    public void ApplyDamage(float amount)
    {
        if (_runtimeStats == null || _isDead)
            return;

        float previousHealth = _runtimeStats.CurrentHealth;
        _runtimeStats.ApplyDamage(amount, _resolvedEffects.maxHealth);

        if (!Mathf.Approximately(previousHealth, _runtimeStats.CurrentHealth))
        {
            BroadcastHealth();
        }

        if (_runtimeStats.IsDead())
        {
            _isDead = true;
            OnPlayerDied?.Invoke();
        }
    }

    public void RestoreHealth(float amount)
    {
        if (_runtimeStats == null)
            return;

        float previousHealth = _runtimeStats.CurrentHealth;
        _runtimeStats.RestoreHealth(amount, _resolvedEffects.maxHealth);

        if (!Mathf.Approximately(previousHealth, _runtimeStats.CurrentHealth))
        {
            BroadcastHealth();
        }
    }

    public bool TryConsumeStamina(float cost)
    {
        if (_runtimeStats == null)
            return false;

        bool consumed = _runtimeStats.TryConsumeStamina(cost);

        if (consumed)
        {
            BroadcastStamina();
        }

        return consumed;
    }

    public void RestoreStamina(float amount)
    {
        if (_runtimeStats == null)
            return;

        float previousStamina = _runtimeStats.CurrentStamina;
        _runtimeStats.RestoreStamina(amount, _resolvedEffects.maxStamina);

        if (!Mathf.Approximately(previousStamina, _runtimeStats.CurrentStamina))
        {
            BroadcastStamina();
        }
    }

    public void SetCurrentHealth(float value)
    {
        if (_runtimeStats == null)
            return;

        _runtimeStats.SetCurrentHealth(value, _resolvedEffects.maxHealth);
        BroadcastHealth();

        if (!_isDead && _runtimeStats.IsDead())
        {
            _isDead = true;
            OnPlayerDied?.Invoke();
        }
    }

    public void SetCurrentStamina(float value)
    {
        if (_runtimeStats == null)
            return;

        _runtimeStats.SetCurrentStamina(value, _resolvedEffects.maxStamina);
        BroadcastStamina();
    }

    private void HandleResolvedEffectsChanged(PlayerResolvedEffects resolvedEffects)
    {
        float previousMaxHealth = _resolvedEffects.maxHealth;
        float previousMaxStamina = _resolvedEffects.maxStamina;

        float previousHealthRatio =
            previousMaxHealth > 0f && _runtimeStats != null
            ? _runtimeStats.CurrentHealth / previousMaxHealth
            : 1f;

        float previousStaminaRatio =
            previousMaxStamina > 0f && _runtimeStats != null
            ? _runtimeStats.CurrentStamina / previousMaxStamina
            : 1f;

        _resolvedEffects = resolvedEffects;

        if (_runtimeStats != null)
        {
            if (_preserveResourceRatiosWhenEffectsChange)
            {
                _runtimeStats.SetCurrentHealth(_resolvedEffects.maxHealth * previousHealthRatio, _resolvedEffects.maxHealth);
                _runtimeStats.SetCurrentStamina(_resolvedEffects.maxStamina * previousStaminaRatio, _resolvedEffects.maxStamina);
            }
            else
            {
                _runtimeStats.ClampToMaximums(_resolvedEffects.maxHealth, _resolvedEffects.maxStamina);
            }
        }

        OnResolvedEffectsChanged?.Invoke(_resolvedEffects);
        BroadcastAll();
    }

    private void RegenerateStamina(float deltaTime)
    {
        if (_runtimeStats == null)
            return;

        if (_resolvedEffects.staminaRegenPerSecond <= 0f)
            return;

        if (_runtimeStats.CurrentStamina >= _resolvedEffects.maxStamina)
            return;

        float previousStamina = _runtimeStats.CurrentStamina;
        float staminaToRestore = _resolvedEffects.staminaRegenPerSecond * deltaTime;

        _runtimeStats.RestoreStamina(staminaToRestore, _resolvedEffects.maxStamina);

        if (!Mathf.Approximately(previousStamina, _runtimeStats.CurrentStamina))
        {
            BroadcastStamina();
        }
    }

    private void BroadcastAll()
    {
        BroadcastHealth();
        BroadcastStamina();
    }

    private void BroadcastHealth()
    {
        OnHealthChanged?.Invoke(currentHP, maxHP);
    }

    private void BroadcastStamina()
    {
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);

        if (_playerData == null)
            return;

        // Temporary compatibility bridge for existing UI-side code.
        _playerData.SyncMana(currentStamina, maxStamina);
    }
}