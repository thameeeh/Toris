using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    menuName = "WorldGen/Biomes/Site Rules/Necromancer Grave Site Rule",
    fileName = "NecromancerGraveSitePlacementRuleDefinition")]
public sealed class NecromancerGraveSitePlacementRuleDefinition : SitePlacementRuleDefinition
{
    private const uint GravePickSalt = 0xC0DE1234u;
    private const uint GraveCountSalt = 0xC0DECAFEu;
    private const int BaseAttempts = 200;
    private const int AttemptsPerTarget = 250;
    private const int RelaxSteps = 6;
    private const int RelaxSpacingStep = 4;
    private const int RelaxedSpacingFloor = 4;
    private const int RelaxStartIndexBase = 100000;

    [SerializeField] private WorldSiteDefinition necromancerGraveSiteDefinition;
    [SerializeField] private SiteStampDefinition necromancerSiteStamp;

    [Header("Count")]
    [SerializeField, Min(0)] private int minGraveSiteCount = 1;
    [SerializeField, Min(1)] private int maxGraveSiteCount = 3;

    [Header("Placement")]
    [SerializeField, Min(1)] private int graveSiteMinSpacingTiles = 40;
    [SerializeField, Range(0.1f, 1f)] private float gravePlacementRadiusFactor = 0.9f;
    [SerializeField, Min(0)] private int graveAvoidOriginRadiusTiles = 28;

    public override void BuildSites(WorldContext ctx)
    {
        if (necromancerGraveSiteDefinition == null || !necromancerGraveSiteDefinition.IsValid)
            return;

        if (necromancerSiteStamp == null)
            return;

        WorldBuildOutput buildOutput = ctx.BuildOutput;
        if (buildOutput == null)
            return;

        int targetCount = ResolveTargetCount(ctx.ActiveBiome.Seed);
        if (targetCount <= 0)
            return;

        int spacingTiles = Mathf.Max(1, graveSiteMinSpacingTiles);
        Vector2Int originTile = ctx.ActiveBiome.OriginTile;
        float radiusTiles = ctx.ActiveBiome.RadiusTiles * Mathf.Clamp01(gravePlacementRadiusFactor);

        List<Vector2Int> chosenCenters = SitePlacementSampling.PickSpacedCentersInBiomeDisk(
            ctx.ActiveBiome.Seed,
            originTile,
            radiusTiles,
            targetCount,
            spacingTiles,
            Mathf.Max(0, graveAvoidOriginRadiusTiles),
            AttemptsPerTarget,
            BaseAttempts,
            RelaxSteps,
            RelaxSpacingStep,
            RelaxedSpacingFloor,
            RelaxStartIndexBase,
            GravePickSalt,
            candidateTile =>
            {
                Vector2Int localTile = ctx.ActiveBiome.ToLocal(candidateTile);
                return ctx.Mask.IsLand(localTile, ctx);
            });

        for (int i = 0; i < chosenCenters.Count; i++)
        {
            Vector2Int centerTile = chosenCenters[i];
            SiteStamping.ApplyStampDefinition(ctx, centerTile, necromancerSiteStamp);
            buildOutput.RegisterSite(necromancerGraveSiteDefinition, centerTile, ctx.World.chunkSize);
        }

        if (chosenCenters.Count < targetCount)
        {
            Debug.LogWarning(
                $"[NecromancerGraveSiteRule] Only placed {chosenCenters.Count}/{targetCount} grave sites (area too constrained).",
                this);
        }
    }

    private int ResolveTargetCount(int biomeSeed)
    {
        int resolvedMin = Mathf.Max(0, minGraveSiteCount);
        int resolvedMax = Mathf.Max(resolvedMin, maxGraveSiteCount);
        if (resolvedMax == resolvedMin)
            return resolvedMin;

        uint countHash = DeterministicHash.Hash((uint)biomeSeed, resolvedMin, resolvedMax, GraveCountSalt);
        int range = resolvedMax - resolvedMin + 1;
        int offset = Mathf.FloorToInt(DeterministicHash.Hash01(countHash) * range);
        return Mathf.Clamp(resolvedMin + offset, resolvedMin, resolvedMax);
    }
}
