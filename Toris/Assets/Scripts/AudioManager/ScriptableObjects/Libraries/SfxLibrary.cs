using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Audio/SFX Library", fileName = "SfxLibrary")]
public sealed class SfxLibrary : ScriptableObject
{
    [SerializeField] private SfxCategory[] categories;

    private readonly Dictionary<string, SfxDefinition> idToDefinition = new();

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

        if (categories == null) return;

        for (int c = 0; c < categories.Length; c++)
        {
            var category = categories[c];
            if (category == null || category.definitions == null) continue;

            for (int i = 0; i < category.definitions.Length; i++)
            {
                var def = category.definitions[i];
                if (def == null) continue;
                if (string.IsNullOrWhiteSpace(def.Id)) continue;

                if (idToDefinition.ContainsKey(def.Id))
                {
                    Debug.LogError($"Duplicate SFX id '{def.Id}' in category '{category.categoryName}'", this);
                    continue;
                }

                idToDefinition.Add(def.Id, def);
            }
        }
    }

    public bool TryGet(string id, out SfxDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            definition = null;
            return false;
        }

        if (idToDefinition.Count == 0)
            RebuildCache();

        return idToDefinition.TryGetValue(id, out definition);
    }
}
