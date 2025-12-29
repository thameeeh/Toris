using System;
using System.Collections.Generic;
using UnityEditor;

#if UNITY_EDITOR
using UnityEngine;
#endif

[CreateAssetMenu(menuName = "Effects/Effect Library", fileName = "EffectLibrary")]
public sealed class EffectLibrary : ScriptableObject, IEffectCatalog
{
    [SerializeField]
    private List<EffectDefinition> definitions = new();

    private readonly Dictionary<string, EffectDefinition> lookup =
        new(StringComparer.Ordinal);

    public IReadOnlyList<EffectDefinition> Definitions => definitions;

    private void OnEnable()
    {
        BuildLookup();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        BuildLookup();
        ValidateDefinitionsInEditor();
    }
#endif

    public bool TryGetDefinition(string effectId, out EffectDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(effectId))
        {
            definition = null;
            return false;
        }

        if (lookup.TryGetValue(effectId, out definition))
        {
            return true;
        }

        // Fallback to linear search if the dictionary is not built yet (e.g. domain reload order).
        foreach (var entry in definitions)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.Id))
            {
                continue;
            }

            if (string.Equals(entry.Id, effectId, StringComparison.Ordinal))
            {
                definition = entry;
                return true;
            }
        }

        definition = null;
        return false;
    }

    private void BuildLookup()
    {
        lookup.Clear();

#if UNITY_EDITOR
        var duplicateIds = new List<string>();
#endif

        foreach (var entry in definitions)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.Id))
            {
                continue;
            }

            if (lookup.ContainsKey(entry.Id))
            {
#if UNITY_EDITOR
                if (!duplicateIds.Contains(entry.Id))
                {
                    duplicateIds.Add(entry.Id);
                }
#endif
                continue;
            }

            lookup.Add(entry.Id, entry);
        }

#if UNITY_EDITOR
        if (duplicateIds.Count > 0)
        {
            Debug.LogWarning(
                $"EffectLibrary '{name}' has duplicate effect IDs: {string.Join(", ", duplicateIds)}",
                this
            );
        }
#endif
    }

#if UNITY_EDITOR
    private void ValidateDefinitionsInEditor()
    {
        if (definitions == null || definitions.Count == 0)
            return;

        for (int index = 0; index < definitions.Count; index++)
        {
            EffectDefinition definition = definitions[index];
            if (definition == null)
                continue;

            ValidateSingleDefinitionInEditor(definition);
        }
    }
#endif
#if UNITY_EDITOR
    private void ValidateSingleDefinitionInEditor(EffectDefinition definition)
    {
        // ID sanity
        if (string.IsNullOrWhiteSpace(definition.Id))
        {
            Debug.LogWarning(
                $"EffectLibrary '{name}' contains an EffectDefinition with an empty Id.",
                this
            );
        }

        // Prefab sanity
        GameObject prefab = definition.Prefab;
        if (prefab == null)
        {
            Debug.LogWarning(
                $"EffectDefinition '{definition.Id}' has no Prefab assigned.",
                this
            );
            return;
        }

        // Prefab should be a project asset, not a scene object
        PrefabAssetType prefabAssetType = PrefabUtility.GetPrefabAssetType(prefab);
        if (prefabAssetType == PrefabAssetType.NotAPrefab)
        {
            Debug.LogWarning(
                $"EffectDefinition '{definition.Id}' Prefab reference is not a prefab asset. Assign a prefab from the Project window.",
                this
            );
        }

        // Pool listener sanity
        bool hasAnyPoolListener = prefab.GetComponentInChildren<IEffectPoolListener>(true) != null;
        if (!hasAnyPoolListener)
        {
            Debug.LogWarning(
                $"EffectDefinition '{definition.Id}' Prefab has no component implementing IEffectPoolListener. " +
                $"(This is fine if you rely only on auto-lifetime, but event-driven releases will be unavailable.)",
                this
            );
        }

        // Category mismatch: OneShot with no release path
        if (definition.Category == EffectCategory.OneShot)
        {
            float oneShotLifetimeSeconds = definition.OneShotLifetimeSeconds;

            // If lifetime is 0 and there is no listener, it has no deterministic release trigger.
            if (oneShotLifetimeSeconds <= 0f && !hasAnyPoolListener)
            {
                Debug.LogWarning(
                    $"EffectDefinition '{definition.Id}' is OneShot but has no OneShotLifetimeSeconds and no IEffectPoolListener. " +
                    $"This effect has no release trigger and can leak active instances.",
                    this
                );
            }
        }

        // Pool bounds sanity (Step 2)
        int maxPoolSize = definition.MaxPoolSize;
        int maxInactive = definition.MaxInactive;

        if (maxPoolSize > 0 && maxInactive > maxPoolSize)
        {
            Debug.LogWarning(
                $"EffectDefinition '{definition.Id}' has MaxInactive ({maxInactive}) > MaxPoolSize ({maxPoolSize}). " +
                $"MaxInactive should be <= MaxPoolSize.",
                this
            );
        }

        // Prewarm sanity
        if (definition.PrewarmPool)
        {
            int prewarmCount = definition.PrewarmCount;

            if (prewarmCount <= 0)
            {
                Debug.LogWarning(
                    $"EffectDefinition '{definition.Id}' has PrewarmPool enabled but PrewarmCount is {prewarmCount}.",
                    this
                );
            }

            if (maxPoolSize > 0 && prewarmCount > maxPoolSize)
            {
                Debug.LogWarning(
                    $"EffectDefinition '{definition.Id}' has PrewarmCount ({prewarmCount}) > MaxPoolSize ({maxPoolSize}). " +
                    $"Prewarm will effectively clamp, but you probably want to fix the data.",
                    this
                );
            }
        }
    }
#endif

}
