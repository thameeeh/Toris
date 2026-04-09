using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerEffectSourceController : MonoBehaviour
{
    [Header("Base Effects")]
    [SerializeField] private PlayerBaseEffectsSO _baseEffects;

    private readonly Dictionary<string, IPlayerEffectSource> _activeSources = new();
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

    public void SetSource(IPlayerEffectSource source)
    {
        if (source == null || string.IsNullOrWhiteSpace(source.SourceKey))
        {
            Debug.LogWarning("[PlayerEffectSourceController] Tried to set a null or invalid runtime source.", this);
            return;
        }

        _activeSources[source.SourceKey] = source;
        RebuildResolvedEffects();
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

        if (_activeSources.TryGetValue(sourceKey, out IPlayerEffectSource existingSource) &&
            existingSource is StaticPlayerEffectSource staticSource)
        {
            staticSource.SetEffectDefinition(effectDefinition);
        }
        else
        {
            _activeSources[sourceKey] = new StaticPlayerEffectSource(sourceKey, effectDefinition);
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

        if (_activeSources.TryGetValue(sourceKey, out IPlayerEffectSource source) &&
            source is StaticPlayerEffectSource staticSource)
        {
            return staticSource.EffectDefinition;
        }

        return null;
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

        foreach (KeyValuePair<string, IPlayerEffectSource> pair in _activeSources)
        {
            pair.Value.CollectModifiers(_cachedModifiers);
        }
    }
}
