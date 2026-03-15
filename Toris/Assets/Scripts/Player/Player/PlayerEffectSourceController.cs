using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerEffectSourceController : MonoBehaviour
{
    [Header("Base Effects")]
    [SerializeField] private PlayerBaseEffectsSO _baseEffects;

    private readonly Dictionary<string, PlayerEffectSourceRuntime> _activeSources = new();
    private readonly List<PlayerEffectModifier> _cachedModifiers = new();

    private PlayerResolvedEffects _resolvedEffects;

    public event Action<PlayerResolvedEffects> OnResolvedEffectsChanged;

    public PlayerResolvedEffects ResolvedEffects => _resolvedEffects;
    public PlayerBaseEffectsSO BaseEffects => _baseEffects;

    private void Awake()
    {
        RebuildResolvedEffects();
    }

    private void OnValidate()
    {
        if (_baseEffects == null)
        {
            Debug.LogWarning($"[PlayerEffectSourceController] Missing PlayerBaseEffectsSO on {name}. Defaults will be used.", this);
        }
    }

    public void SetSource(string sourceKey, PlayerEffectDefinitionSO effectDefinition)
    {
        if (string.IsNullOrWhiteSpace(sourceKey))
        {
            Debug.LogWarning("[PlayerEffectSourceController] Tried to set a source with an empty key.", this);
            return;
        }

        if (effectDefinition == null)
        {
            RemoveSource(sourceKey);
            return;
        }

        if (_activeSources.TryGetValue(sourceKey, out PlayerEffectSourceRuntime existingSource))
        {
            existingSource.SetEffectDefinition(effectDefinition);
        }
        else
        {
            _activeSources[sourceKey] = new PlayerEffectSourceRuntime(sourceKey, effectDefinition);
        }

        RebuildResolvedEffects();
    }

    public void RemoveSource(string sourceKey)
    {
        if (string.IsNullOrWhiteSpace(sourceKey))
            return;

        if (_activeSources.Remove(sourceKey))
        {
            RebuildResolvedEffects();
        }
    }

    public void ClearAllSources()
    {
        if (_activeSources.Count == 0)
            return;

        _activeSources.Clear();
        RebuildResolvedEffects();
    }

    public bool HasSource(string sourceKey)
    {
        return !string.IsNullOrWhiteSpace(sourceKey) && _activeSources.ContainsKey(sourceKey);
    }

    public PlayerEffectDefinitionSO GetSourceDefinition(string sourceKey)
    {
        if (string.IsNullOrWhiteSpace(sourceKey))
            return null;

        return _activeSources.TryGetValue(sourceKey, out PlayerEffectSourceRuntime sourceRuntime)
            ? sourceRuntime.EffectDefinition
            : null;
    }

    public void RebuildResolvedEffects()
    {
        CollectModifiers();
        _resolvedEffects = PlayerEffectResolver.Resolve(_baseEffects, _cachedModifiers);
        OnResolvedEffectsChanged?.Invoke(_resolvedEffects);
    }

    private void CollectModifiers()
    {
        _cachedModifiers.Clear();

        foreach (KeyValuePair<string, PlayerEffectSourceRuntime> pair in _activeSources)
        {
            pair.Value.CollectModifiers(_cachedModifiers);
        }
    }
}