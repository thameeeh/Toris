using UnityEngine;

[CreateAssetMenu(menuName = "WorldGen/Biome Database", fileName = "BiomeDatabase")]
public sealed class BiomeDatabase : ScriptableObject
{
    public BiomeDefinition[] biomes;

    public int Count => biomes != null ? biomes.Length : 0;
    public BiomeDefinition Get(int index)
    {
        if (biomes == null || biomes.Length == 0) return null;
        index = Mathf.Clamp(index, 0, biomes.Length - 1);
        return biomes[index];
    }
}
