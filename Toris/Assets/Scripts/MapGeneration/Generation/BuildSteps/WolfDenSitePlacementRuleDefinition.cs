using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(
    menuName = "WorldGen/Biomes/Site Rules/Wolf Den Site Rule",
    fileName = "WolfDenSitePlacementRuleDefinition")]
public sealed class WolfDenSitePlacementRuleDefinition : SitePlacementRuleDefinition
{
    private const uint DenPickSalt = 0xD311C0DEu;
    private const uint DenCountSalt = 0xD311C001u;
    private const int AvoidOriginRadiusTiles = 18;
    private const int BaseAttempts = 200;
    private const int AttemptsPerTarget = 250;
    private const int RelaxSteps = 6;
    private const int RelaxSpacingStep = 4;
    private const int RelaxedSpacingFloor = 4;
    private const int RelaxStartIndexBase = 100000;

    [SerializeField] private WorldSiteDefinition wolfDenSiteDefinition;
    [SerializeField] private SiteStampDefinition wolfDenStampDefinition;
    [SerializeField]
    [Min(0)] private int minWolfDenCount = 3;
    [SerializeField]
    [Min(0)] private int maxWolfDenCount = 3;
    [SerializeField]
    [Min(1)] private int wolfDenMinSpacingTiles = 40;
    [SerializeField] private TileBase wolfDenGroundTile;
    [SerializeField]
    [Range(1, 15)] private int wolfDenStampSize = 5;

    public override void BuildSites(WorldContext ctx)
    {
        if (wolfDenSiteDefinition == null || !wolfDenSiteDefinition.IsValid)
            return;

        WorldBuildOutput buildOutput = ctx.BuildOutput;
        if (buildOutput == null)
            return;

        int targetCount = ResolveTargetCount(ctx.ActiveBiome.Seed);
        if (targetCount == 0)
            return;

        int spacingTiles = Mathf.Max(1, wolfDenMinSpacingTiles);
        int stampSize = Mathf.Max(1, wolfDenStampSize);

        Vector2Int originTile = ctx.ActiveBiome.OriginTile;
        float radiusTiles = ctx.ActiveBiome.RadiusTiles * 0.90f;

        List<Vector2Int> chosenCenters = SitePlacementSampling.PickSpacedCentersInBiomeDisk(
            ctx.ActiveBiome.Seed,
            originTile,
            radiusTiles,
            targetCount,
            spacingTiles,
            AvoidOriginRadiusTiles,
            AttemptsPerTarget,
            BaseAttempts,
            RelaxSteps,
            RelaxSpacingStep,
            RelaxedSpacingFloor,
            RelaxStartIndexBase,
            DenPickSalt,
            candidateTile =>
            {
                Vector2Int localTile = ctx.ActiveBiome.ToLocal(candidateTile);
                return ctx.Mask.IsLand(localTile, ctx);
            });

        for (int i = 0; i < chosenCenters.Count; i++)
        {
            Vector2Int centerTile = chosenCenters[i];

            if (wolfDenStampDefinition != null)
                SiteStamping.ApplyStampDefinition(ctx, centerTile, wolfDenStampDefinition);
            else
            {
                SiteStamping.StampSquareGround(
                    ctx,
                    centerTile,
                    stampSize,
                    wolfDenGroundTile);

                SiteStamping.AddSquareBlocker(
                    ctx,
                    centerTile,
                    stampSize);
            }

            buildOutput.RegisterSite(wolfDenSiteDefinition, centerTile, ctx.World.chunkSize);
        }

#if UNITY_EDITOR
        if (chosenCenters.Count < targetCount)
        {
            Debug.LogWarning(
                $"[WolfDenSiteRule] Only placed {chosenCenters.Count}/{targetCount} dens (area too constrained).",
                this);
        }
#endif
    }

    private int ResolveTargetCount(int biomeSeed)
    {
        int resolvedMin = Mathf.Max(0, minWolfDenCount);
        int resolvedMax = Mathf.Max(resolvedMin, maxWolfDenCount);
        if (resolvedMax == resolvedMin)
            return resolvedMin;

        uint countHash = DeterministicHash.Hash((uint)biomeSeed, resolvedMin, resolvedMax, DenCountSalt);
        int range = resolvedMax - resolvedMin + 1;
        int offset = Mathf.FloorToInt(DeterministicHash.Hash01(countHash) * range);
        return Mathf.Clamp(resolvedMin + offset, resolvedMin, resolvedMax);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        minWolfDenCount = Mathf.Max(0, minWolfDenCount);
        maxWolfDenCount = Mathf.Max(minWolfDenCount, maxWolfDenCount);
        wolfDenMinSpacingTiles = Mathf.Max(1, wolfDenMinSpacingTiles);
        wolfDenStampSize = Mathf.Clamp(wolfDenStampSize, 1, 15);
    }
#endif
}
