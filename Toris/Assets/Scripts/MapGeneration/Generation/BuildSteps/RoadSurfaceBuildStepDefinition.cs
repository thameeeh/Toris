using UnityEngine;

[CreateAssetMenu(
    menuName = "WorldGen/Biomes/Build Steps/Road Surface Step",
    fileName = "RoadSurfaceBuildStepDefinition")]
public sealed class RoadSurfaceBuildStepDefinition : BiomeBuildStepDefinition
{
    public override void Build(WorldContext ctx)
    {
        RoadSurfaceBuilder.Build(ctx);
    }
}