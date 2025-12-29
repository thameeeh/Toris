using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Audio/Music Library", fileName = "MusicLibrary")]
public sealed class MusicLibrary : ScriptableObject
{
    [SerializeField] private MusicDefinition[] definitions;

    private readonly Dictionary<string, MusicDefinition> idToDefinition = new Dictionary<string, MusicDefinition>();

    public void RebuildCache()
    {
        idToDefinition.Clear();

        if (definitions == null) return;

        for (int i = 0; i < definitions.Length; i++)
        {
            MusicDefinition definition = definitions[i];
            if (definition == null) continue;
            if (string.IsNullOrWhiteSpace(definition.Id)) continue;

            if (idToDefinition.ContainsKey(definition.Id))
            {
                Debug.LogError($"MusicLibrary has duplicate id: '{definition.Id}'.", this);
                continue;
            }

            idToDefinition.Add(definition.Id, definition);
        }
    }

    public bool TryGet(string id, out MusicDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            definition = null;
            return false;
        }

        if (idToDefinition.Count == 0) RebuildCache();
        return idToDefinition.TryGetValue(id, out definition);
    }
}
