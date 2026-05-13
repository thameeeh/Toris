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
    private const int BaseAttempts = 200;
    private const int AttemptsPerTarget = 250;
    private const int RelaxSteps = 6;
    private const int RelaxSpacingStep = 4;
    private const int RelaxedSpacingFloor = 4;
    private const int RelaxStartIndexBase = 100000;

    [Header("Site")]
    [SerializeField] private WorldSiteDefinition wolfDenSiteDefinition;
    [SerializeField] private SiteStampDefinition wolfDenStampDefinition;
    [SerializeField] private TileBase wolfDenGroundTile;
    [SerializeField, Range(1, 15)] private int wolfDenStampSize = 5;

    [Header("Count")]
    [SerializeField, Min(0)] private int minWolfDenCount = 3;
    [SerializeField, Min(0)] private int maxWolfDenCount = 3;

    [Header("Placement")]
    [SerializeField, Min(1)] private int wolfDenMinSpacingTiles = 40;
    [SerializeField, Range(0.1f, 1f)] private float placementRadiusFactor = 0.9f;
    [SerializeField, Min(0)] private int avoidOriginRadiusTiles = 18;
    [SerializeField] private bool avoidExistingWolfDens = true;
    [SerializeField] private bool avoidExistingStamps = true;

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
        float radiusTiles = ctx.ActiveBiome.RadiusTiles * Mathf.Clamp(placementRadiusFactor, 0.1f, 1f);

        List<Vector2Int> chosenCenters = SitePlacementSampling.PickSpacedCentersInBiomeDisk(
            ctx.ActiveBiome.Seed,
            originTile,
            radiusTiles,
            targetCount,
            spacingTiles,
            Mathf.Max(0, avoidOriginRadiusTiles),
            AttemptsPerTarget,
            BaseAttempts,
            RelaxSteps,
            RelaxSpacingStep,
            RelaxedSpacingFloor,
            RelaxStartIndexBase,
            DenPickSalt,
            candidateTile =>
            {
                return IsValidCandidateTile(ctx, candidateTile, spacingTiles);
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

    private bool IsValidCandidateTile(WorldContext ctx, Vector2Int candidateTile, int spacingTiles)
    {
        Vector2Int localTile = ctx.ActiveBiome.ToLocal(candidateTile);
        if (!ctx.Mask.IsLand(localTile, ctx))
            return false;

        if (HasBlockedExistingStamp(ctx, candidateTile))
            return false;

        return !HasNearbyExistingWolfDen(ctx, candidateTile, spacingTiles);
    }

    private bool HasBlockedExistingStamp(WorldContext ctx, Vector2Int candidateTile)
    {
        if (!avoidExistingStamps || ctx?.BuildOutput?.TerrainOverrides == null)
            return false;

        return ctx.BuildOutput.TerrainOverrides.TryGet(candidateTile, out _);
    }

    private bool HasNearbyExistingWolfDen(WorldContext ctx, Vector2Int candidateTile, int spacingTiles)
    {
        if (!avoidExistingWolfDens || ctx?.BuildOutput?.SitePlacements?.All == null || wolfDenSiteDefinition == null)
            return false;

        int spacingTilesSquared = Mathf.Max(1, spacingTiles) * Mathf.Max(1, spacingTiles);
        IReadOnlyList<SitePlacement> placements = ctx.BuildOutput.SitePlacements.All;
        for (int i = 0; i < placements.Count; i++)
        {
            SitePlacement placement = placements[i];
            if (placement.SiteDefinition != wolfDenSiteDefinition)
                continue;

            if ((placement.CenterTile - candidateTile).sqrMagnitude < spacingTilesSquared)
                return true;
        }

        return false;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        minWolfDenCount = Mathf.Max(0, minWolfDenCount);
        maxWolfDenCount = Mathf.Max(minWolfDenCount, maxWolfDenCount);
        wolfDenMinSpacingTiles = Mathf.Max(1, wolfDenMinSpacingTiles);
        wolfDenStampSize = Mathf.Clamp(wolfDenStampSize, 1, 15);
        placementRadiusFactor = Mathf.Clamp(placementRadiusFactor, 0.1f, 1f);
        avoidOriginRadiusTiles = Mathf.Max(0, avoidOriginRadiusTiles);
    }
#endif
}
