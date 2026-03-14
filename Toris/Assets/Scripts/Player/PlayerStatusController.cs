using System;
using System.Collections.Generic;
using UnityEngine;

// PURPOSE:
// - Own active runtime player status effects (poison / burning / bleeding)
// - Apply DOT through PlayerStats
// - Respect immunity flags from PlayerStats.ResolvedEffects
// - Expose clean methods for gameplay systems to apply/remove/query statuses

public class PlayerStatusController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerStats _playerStats;

    private readonly Dictionary<PlayerStatusEffectType, PlayerStatusInstance> _activeStatuses = new();

    public event Action<PlayerStatusEffectType> OnStatusApplied;
    public event Action<PlayerStatusEffectType> OnStatusRemoved;
    public event Action<PlayerStatusEffectType, float> OnStatusDamageTick;

    public bool HasStatus(PlayerStatusEffectType statusType) => _activeStatuses.ContainsKey(statusType);

    private void OnValidate()
    {
        if (_playerStats == null)
        {
            Debug.LogWarning($"[PlayerStatusController] Missing PlayerStats on {name}", this);
        }
    }

    private void Update()
    {
        if (_playerStats == null || _activeStatuses.Count == 0)
            return;

        TickStatuses(Time.deltaTime);
    }

    public bool TryApplyStatus(
        PlayerStatusEffectType statusType,
        float damagePerSecond,
        float duration,
        float tickInterval = 1f,
        int stacks = 1)
    {
        if (_playerStats == null)
            return false;

        if (IsImmune(statusType))
            return false;

        if (_activeStatuses.TryGetValue(statusType, out PlayerStatusInstance existingStatus))
        {
            existingStatus.Refresh(
                damagePerSecond,
                duration,
                tickInterval,
                stacks);

            return true;
        }

        PlayerStatusInstance newStatus = new PlayerStatusInstance();
        newStatus.Initialize(
            statusType,
            damagePerSecond,
            duration,
            tickInterval,
            stacks);

        _activeStatuses[statusType] = newStatus;
        OnStatusApplied?.Invoke(statusType);
        return true;
    }

    public bool RemoveStatus(PlayerStatusEffectType statusType)
    {
        if (_activeStatuses.Remove(statusType))
        {
            OnStatusRemoved?.Invoke(statusType);
            return true;
        }

        return false;
    }

    public void ClearAllStatuses()
    {
        if (_activeStatuses.Count == 0)
            return;

        List<PlayerStatusEffectType> keys = new List<PlayerStatusEffectType>(_activeStatuses.Keys);
        _activeStatuses.Clear();

        for (int i = 0; i < keys.Count; i++)
        {
            OnStatusRemoved?.Invoke(keys[i]);
        }
    }

    public float GetRemainingDuration(PlayerStatusEffectType statusType)
    {
        return _activeStatuses.TryGetValue(statusType, out PlayerStatusInstance status)
            ? status.RemainingDuration
            : 0f;
    }

    public int GetStacks(PlayerStatusEffectType statusType)
    {
        return _activeStatuses.TryGetValue(statusType, out PlayerStatusInstance status)
            ? status.Stacks
            : 0;
    }

    private void TickStatuses(float deltaTime)
    {
        List<PlayerStatusEffectType> expiredStatuses = null;

        foreach (KeyValuePair<PlayerStatusEffectType, PlayerStatusInstance> pair in _activeStatuses)
        {
            PlayerStatusEffectType statusType = pair.Key;
            PlayerStatusInstance status = pair.Value;

            if (status.Tick(deltaTime, out float damageToApply))
            {
                if (damageToApply > 0f)
                {
                    _playerStats.ApplyDamage(damageToApply);
                    OnStatusDamageTick?.Invoke(statusType, damageToApply);
                }
            }

            if (status.IsExpired)
            {
                expiredStatuses ??= new List<PlayerStatusEffectType>();
                expiredStatuses.Add(statusType);
            }
        }

        if (expiredStatuses == null)
            return;

        for (int i = 0; i < expiredStatuses.Count; i++)
        {
            PlayerStatusEffectType expiredType = expiredStatuses[i];
            _activeStatuses.Remove(expiredType);
            OnStatusRemoved?.Invoke(expiredType);
        }
    }

    private bool IsImmune(PlayerStatusEffectType statusType)
    {
        PlayerResolvedEffects resolvedEffects = _playerStats.ResolvedEffects;

        return statusType switch
        {
            PlayerStatusEffectType.Poison => resolvedEffects.isPoisonImmune,
            PlayerStatusEffectType.Burning => resolvedEffects.isBurningImmune,
            PlayerStatusEffectType.Bleeding => resolvedEffects.isBleedingImmune,
            _ => false
        };
    }

    [ContextMenu("Test Poison")]
    private void TestPoison()
    {
        TryApplyStatus(PlayerStatusEffectType.Poison, 5f, 5f, 1f, 1);
    }

    [ContextMenu("Test Burning")]
    private void TestBurning()
    {
        TryApplyStatus(PlayerStatusEffectType.Burning, 8f, 4f, 1f, 1);
    }

    [ContextMenu("Test Bleeding")]
    private void TestBleeding()
    {
        TryApplyStatus(PlayerStatusEffectType.Bleeding, 3f, 6f, 1f, 2);
    }

    [ContextMenu("Clear Statuses")]
    private void DebugClearStatuses()
    {
        ClearAllStatuses();
    }
}