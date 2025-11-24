using System;
using System.Collections.Generic;
using UnityEngine;

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
                this);
        }
#endif
    }
}
