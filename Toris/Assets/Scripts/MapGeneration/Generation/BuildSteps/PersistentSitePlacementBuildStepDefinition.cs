using UnityEngine;

[CreateAssetMenu(
    menuName = "WorldGen/Biomes/Build Steps/Persistent Site Placement Step",
    fileName = "PersistentSitePlacementBuildStepDefinition")]
public sealed class PersistentSitePlacementBuildStepDefinition : BiomeBuildStepDefinition
{
    [SerializeField] private PersistentBiomeFeatureDefinition[] persistentFeatures;

    public override void Build(WorldContext ctx)
    {
        if (ctx == null || persistentFeatures == null || persistentFeatures.Length == 0)
            return;

        WorldBuildOutput buildOutput = ctx.BuildOutput;
        if (buildOutput == null)
            return;

        int chunkSize = Mathf.Max(1, ctx.World.chunkSize);
        Vector2Int biomeOriginTile = ctx.ActiveBiome.OriginTile;

        for (int i = 0; i < persistentFeatures.Length; i++)
        {
            PersistentBiomeFeatureDefinition persistentFeature = persistentFeatures[i];
            if (!persistentFeature.IsValid)
                continue;

            Vector2Int centerTile = biomeOriginTile + persistentFeature.TileOffsetFromBiomeOrigin;
            buildOutput.RegisterSite(
                persistentFeature.SiteDefinition,
                centerTile,
                chunkSize,
                SitePlacementLifecycleScope.PersistentBiome);
        }
    }
}
