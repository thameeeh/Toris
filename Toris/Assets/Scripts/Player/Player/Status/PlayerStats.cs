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
    [SerializeField] private PlayerStatsAnchorSO _playerStatsAnchor;
    [SerializeField] private OutlandHaven.UIToolkit.GameSessionSO _gameSession;

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
    public float healthRegenPerSec => _resolvedEffects.healthRegenPerSecond;

    public float maxStamina => _resolvedEffects.maxStamina;
    public float currentStamina => _runtimeStats != null ? _runtimeStats.CurrentStamina : _resolvedEffects.maxStamina;

    public float staminaRegenPerSec => _resolvedEffects.staminaRegenPerSecond;

    private void Awake()
    {
        _runtimeStats = new PlayerRuntimeStats();
        _resolvedEffects = ResolveValidResolvedEffects();

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

        if (_playerStatsAnchor != null)
        {
            _playerStatsAnchor.Provide(this);
        }

        RefreshResolvedEffectsFromController(false);
        TryRestoreTransferredState();
    }

    private void OnDisable()
    {
        CaptureTransferredState();

        if (_effectSourceController != null)
        {
            _effectSourceController.OnResolvedEffectsChanged -= HandleResolvedEffectsChanged;
        }

        if (_playerStatsAnchor != null)
        {
            _playerStatsAnchor.Clear();
        }
    }

    private void Update()
    {
        RegenerateHealth(Time.deltaTime);
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

    public void SetRuntimeState(float currentHealthValue, float currentStaminaValue)
    {
        if (_runtimeStats == null)
            return;

        _runtimeStats.SetCurrentHealth(currentHealthValue, _resolvedEffects.maxHealth);
        _runtimeStats.SetCurrentStamina(currentStaminaValue, _resolvedEffects.maxStamina);
        _isDead = _runtimeStats.IsDead();
        BroadcastAll();
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

    private void RegenerateHealth(float deltaTime)
    {
        if (_runtimeStats == null)
            return;

        if (_resolvedEffects.healthRegenPerSecond <= 0f)
            return;

        if (_runtimeStats.CurrentHealth >= _resolvedEffects.maxHealth)
            return;

        float previousHealth = _runtimeStats.CurrentHealth;
        float healthToRestore = _resolvedEffects.healthRegenPerSecond * deltaTime;

        _runtimeStats.RestoreHealth(healthToRestore, _resolvedEffects.maxHealth);

        if (!Mathf.Approximately(previousHealth, _runtimeStats.CurrentHealth))
        {
            BroadcastHealth();
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
    }

    private void TryRestoreTransferredState()
    {
        ResolveGameSession();

        if (_gameSession == null || _runtimeStats == null)
            return;

        if (!_gameSession.TryGetPlayerStatsState(out float currentHealthValue, out float currentStaminaValue))
            return;

        SetRuntimeState(currentHealthValue, currentStaminaValue);
    }

    private void CaptureTransferredState()
    {
        ResolveGameSession();

        if (_gameSession == null || _runtimeStats == null)
            return;

        _gameSession.CapturePlayerStatsState(currentHP, currentStamina);
    }

    private void ResolveGameSession()
    {
        if (_gameSession == null)
            _gameSession = OutlandHaven.UIToolkit.GameSessionSO.LoadDefault();
    }

    private void RefreshResolvedEffectsFromController(bool preserveRuntimeResources)
    {
        PlayerResolvedEffects refreshedEffects = ResolveValidResolvedEffects();

        if (_runtimeStats == null)
        {
            _resolvedEffects = refreshedEffects;
            return;
        }

        if (preserveRuntimeResources)
        {
            HandleResolvedEffectsChanged(refreshedEffects);
            return;
        }

        _resolvedEffects = refreshedEffects;
        _runtimeStats.ClampToMaximums(_resolvedEffects.maxHealth, _resolvedEffects.maxStamina);
        _isDead = _runtimeStats.IsDead();
        OnResolvedEffectsChanged?.Invoke(_resolvedEffects);
        BroadcastAll();
    }

    private PlayerResolvedEffects ResolveValidResolvedEffects()
    {
        if (_effectSourceController == null)
            return PlayerResolvedEffects.CreateDefault();

        PlayerResolvedEffects resolvedEffects = _effectSourceController.ResolvedEffects;
        if (!IsResolvedEffectsInitialized(resolvedEffects))
        {
            _effectSourceController.RebuildResolvedEffects();
            resolvedEffects = _effectSourceController.ResolvedEffects;
        }

        return IsResolvedEffectsInitialized(resolvedEffects)
            ? resolvedEffects
            : PlayerEffectResolver.CreateFromBase(_effectSourceController.BaseEffects);
    }

    private static bool IsResolvedEffectsInitialized(PlayerResolvedEffects resolvedEffects)
    {
        return resolvedEffects.maxHealth > 0f;
    }
}
