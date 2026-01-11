using UnityEngine;

[CreateAssetMenu(menuName = "WorldGen/Biomes/Basic Biome", fileName = "BasicBiomeDefinition")]
public sealed class BasicBiomeDefinition : BiomeDefinition
{
    public override void BuildFeatures(WorldContext ctx)
    {
        BiomeFeatureBuilder.Build(ctx);
        DenFeatureBuilder.Build(ctx);
    }
}
