using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Audio/SFX Library", fileName = "SfxLibrary")]
public sealed class SfxLibrary : ScriptableObject
{
    [SerializeField] private SfxDefinition[] definitions;

    private readonly Dictionary<string, SfxDefinition> idToDefinition = new Dictionary<string, SfxDefinition>();
    private void OnEnable()
    {
        RebuildCache();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        RebuildCache();
    }
#endif

    public void RebuildCache()
    {
        idToDefinition.Clear();

        if (definitions == null) return;

        for (int i = 0; i < definitions.Length; i++)
        {
            SfxDefinition definition = definitions[i];
            if (definition == null) continue;
            if (string.IsNullOrWhiteSpace(definition.Id)) continue;

            if (idToDefinition.ContainsKey(definition.Id))
            {
                Debug.LogError($"SfxLibrary has duplicate id: '{definition.Id}'.", this);
                continue;
            }

            idToDefinition.Add(definition.Id, definition);
        }
    }

    public bool TryGet(string id, out SfxDefinition definition)
    {
        definition = null;
        if (string.IsNullOrWhiteSpace(id)) return false;

        if (idToDefinition.Count == 0)
            RebuildCache();

        if (idToDefinition.TryGetValue(id, out definition))
            return true;

        RebuildCache();
        return idToDefinition.TryGetValue(id, out definition);
    }

}
