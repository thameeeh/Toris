using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(
    menuName = "WorldGen/Biomes/Site Rules/Wolf Den Site Rule",
    fileName = "WolfDenSitePlacementRuleDefinition")]
public sealed class WolfDenSitePlacementRuleDefinition : SitePlacementRuleDefinition
{
    private const uint DenPickSalt = 0xD311C0DEu;
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

        int targetMin = Mathf.Max(0, minWolfDenCount);
        if (targetMin == 0)
            return;

        int spacingTiles = Mathf.Max(1, wolfDenMinSpacingTiles);
        int stampSize = Mathf.Max(1, wolfDenStampSize);

        Vector2Int originTile = ctx.ActiveBiome.OriginTile;
        float radiusTiles = ctx.ActiveBiome.RadiusTiles * 0.90f;

        List<Vector2Int> chosenCenters = SitePlacementSampling.PickSpacedCentersInBiomeDisk(
            ctx.ActiveBiome.Seed,
            originTile,
            radiusTiles,
            targetMin,
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

        if (chosenCenters.Count < targetMin)
        {
            Debug.LogWarning(
                $"[WolfDenSiteRule] Only placed {chosenCenters.Count}/{targetMin} dens (area too constrained).",
                this);
        }
    }
}
