using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    menuName = "WorldGen/Biomes/Site Rules/Boar Oasis Site Rule",
    fileName = "BoarOasisSitePlacementRuleDefinition")]
public sealed class BoarOasisSitePlacementRuleDefinition : SitePlacementRuleDefinition
{
    private const uint OasisPickSalt = 0xB0A20A51u;
    private const uint OasisCountSalt = 0xB0A2C001u;
    private const uint LayoutVariantSelectionSalt = 0xB0A2CAFEu;
    private const int BaseAttempts = 200;
    private const int AttemptsPerTarget = 250;
    private const int RelaxSteps = 6;
    private const int RelaxSpacingStep = 4;
    private const int RelaxedSpacingFloor = 4;
    private const int RelaxStartIndexBase = 100000;
    private const float MinPlacementRadiusFactor = 0.1f;

    [Header("Site")]
    [SerializeField] private WorldSiteDefinition boarOasisSiteDefinition;
    [SerializeField] private SiteStampDefinition boarOasisStampDefinition;

    [Header("Count")]
    [SerializeField, Min(0)] private int minSiteCount = 0;
    [SerializeField, Min(0)] private int maxSiteCount = 2;

    [Header("Placement")]
    [SerializeField, Min(1)] private int minSpacingTiles = 56;
    [SerializeField, Range(0.1f, 1f)] private float placementRadiusFactor = 0.85f;
    [SerializeField, Min(0)] private int avoidOriginRadiusTiles = 50;
    [SerializeField] private bool avoidExistingStamps = true;
    [SerializeField] private bool avoidExistingSites = true;
    [SerializeField] private bool avoidRoadTiles = true;
    [SerializeField, Min(0)] private int roadSpacingTiles = 0;
    [SerializeField] private bool avoidTerrainOverrides = true;
    [SerializeField] private bool avoidNavigationBlockers = true;
    [SerializeField] private bool avoidObstacles = true;

    [System.NonSerialized] private TileResolver tileResolver;

    public override void BuildSites(WorldContext ctx)
    {
        if (ctx == null || ctx.BuildOutput == null)
            return;

        if (boarOasisSiteDefinition == null || !boarOasisSiteDefinition.IsValid)
            return;

        if (!HasStampContent(boarOasisStampDefinition))
            return;

        int targetCount = ResolveTargetCount(ctx.ActiveBiome.Seed);
        if (targetCount <= 0)
            return;

        int spacingTiles = Mathf.Max(1, minSpacingTiles);
        float radiusTiles = ctx.ActiveBiome.RadiusTiles * Mathf.Clamp(
            placementRadiusFactor,
            MinPlacementRadiusFactor,
            1f);

        List<Vector2Int> chosenCenters = SitePlacementSampling.PickSpacedCentersInBiomeDisk(
            ctx.ActiveBiome.Seed,
            ctx.ActiveBiome.OriginTile,
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
            OasisPickSalt,
            candidateTile => IsValidCandidateTile(ctx, candidateTile, spacingTiles));

        for (int i = 0; i < chosenCenters.Count; i++)
        {
            Vector2Int centerTile = chosenCenters[i];
            SiteStamping.ApplyStampDefinition(ctx, centerTile, boarOasisStampDefinition);

            ctx.BuildOutput.RegisterSite(boarOasisSiteDefinition, centerTile, ctx.World.chunkSize);
        }

#if UNITY_EDITOR
        if (chosenCenters.Count < targetCount)
        {
            Debug.LogWarning(
                $"[BoarOasisSiteRule] Only placed {chosenCenters.Count}/{targetCount} Boar Oasis sites (area too constrained).",
                this);
        }
#endif
    }

    private int ResolveTargetCount(int biomeSeed)
    {
        int resolvedMin = Mathf.Max(0, minSiteCount);
        int resolvedMax = Mathf.Max(resolvedMin, maxSiteCount);
        if (resolvedMax == resolvedMin)
            return resolvedMin;

        uint countHash = DeterministicHash.Hash(
            (uint)biomeSeed,
            resolvedMin,
            resolvedMax,
            OasisCountSalt);
        int range = resolvedMax - resolvedMin + 1;
        int offset = Mathf.FloorToInt(DeterministicHash.Hash01(countHash) * range);
        return Mathf.Clamp(resolvedMin + offset, resolvedMin, resolvedMax);
    }

    private bool IsValidCandidateTile(WorldContext ctx, Vector2Int candidateTile, int spacingTiles)
    {
        if (HasNearbyExistingSite(ctx, candidateTile, spacingTiles))
            return false;

        return IsStampFootprintAllowed(ctx, candidateTile);
    }

    private bool IsStampFootprintAllowed(WorldContext ctx, Vector2Int centerTile)
    {
        if (!IsTileAllowed(ctx, centerTile))
            return false;

        if (boarOasisStampDefinition != null)
        {
            if (boarOasisStampDefinition.HasGroundStamp)
            {
                if (!IsRectFootprintAllowed(
                        ctx,
                        centerTile + boarOasisStampDefinition.GroundOffset,
                        boarOasisStampDefinition.GroundWidth,
                        boarOasisStampDefinition.GroundHeight))
                {
                    return false;
                }
            }

            if (boarOasisStampDefinition.HasNavigationBlockerStamp)
            {
                if (!IsRectFootprintAllowed(
                        ctx,
                        centerTile + boarOasisStampDefinition.BlockerOffset,
                        boarOasisStampDefinition.BlockerWidth,
                        boarOasisStampDefinition.BlockerHeight))
                {
                    return false;
                }
            }

            if (boarOasisStampDefinition.HasVisualClearZone)
            {
                if (!IsRectFootprintAllowed(
                        ctx,
                        centerTile + boarOasisStampDefinition.ClearVisualsOffset,
                        boarOasisStampDefinition.ClearVisualsWidth,
                        boarOasisStampDefinition.ClearVisualsHeight))
                {
                    return false;
                }
            }

            SiteTileLayoutDefinition layoutDefinition = ResolveLayoutDefinition(ctx, centerTile);
            IReadOnlyList<SiteTileLayoutCell> cells = layoutDefinition != null ? layoutDefinition.Cells : null;
            if (cells != null)
            {
                for (int i = 0; i < cells.Count; i++)
                {
                    SiteTileLayoutCell cell = cells[i];
                    if (!HasPaintedTile(cell))
                        continue;

                    if (!IsTileAllowed(ctx, centerTile + cell.offset))
                        return false;
                }
            }
        }

        return true;
    }

    private bool IsRectFootprintAllowed(
        WorldContext ctx,
        Vector2Int centerTile,
        int width,
        int height)
    {
        int halfWidth = Mathf.Max(1, width) / 2;
        int halfHeight = Mathf.Max(1, height) / 2;

        for (int y = -halfHeight; y <= halfHeight; y++)
        {
            for (int x = -halfWidth; x <= halfWidth; x++)
            {
                if (!IsTileAllowed(ctx, centerTile + new Vector2Int(x, y)))
                    return false;
            }
        }

        return true;
    }

    private bool IsTileAllowed(WorldContext ctx, Vector2Int worldTile)
    {
        Vector2Int localTile = ctx.ActiveBiome.ToLocal(worldTile);
        if (!ctx.Mask.IsLand(localTile, ctx))
            return false;

        if (HasBlockedTerrainOverride(ctx, worldTile))
            return false;

        if (avoidNavigationBlockers
            && ctx.BuildOutput.NavigationContributions != null
            && ctx.BuildOutput.NavigationContributions.GetNavigationContribution(worldTile).BlocksNavigation)
        {
            return false;
        }

        TileResult tileResult = GetTileResolver().Resolve(worldTile, ctx);
        if (tileResult.ground == null || tileResult.HasWater)
            return false;

        if (avoidObstacles && tileResult.HasObstacle)
            return false;

        return true;
    }

    private bool HasBlockedTerrainOverride(WorldContext ctx, Vector2Int worldTile)
    {
        if (ctx?.BuildOutput?.TerrainOverrides == null)
            return false;

        if (!avoidExistingStamps && !avoidRoadTiles && !avoidTerrainOverrides)
            return false;

        int spacingTiles = avoidRoadTiles ? Mathf.Max(0, roadSpacingTiles) : 0;
        for (int y = -spacingTiles; y <= spacingTiles; y++)
        {
            for (int x = -spacingTiles; x <= spacingTiles; x++)
            {
                Vector2Int offset = new Vector2Int(x, y);
                if (offset.sqrMagnitude > spacingTiles * spacingTiles)
                    continue;

                if (ctx.BuildOutput.TerrainOverrides.TryGet(worldTile + offset, out _))
                    return true;
            }
        }

        return false;
    }

    private bool HasNearbyExistingSite(WorldContext ctx, Vector2Int candidateTile, int spacingTiles)
    {
        if (!avoidExistingSites || ctx?.BuildOutput?.SitePlacements?.All == null)
            return false;

        int spacingTilesSquared = spacingTiles * spacingTiles;
        IReadOnlyList<SitePlacement> placements = ctx.BuildOutput.SitePlacements.All;
        for (int i = 0; i < placements.Count; i++)
        {
            SitePlacement placement = placements[i];
            if (placement.SiteDefinition == null)
                continue;

            if ((placement.CenterTile - candidateTile).sqrMagnitude < spacingTilesSquared)
                return true;
        }

        return false;
    }

    private SiteTileLayoutDefinition ResolveLayoutDefinition(WorldContext ctx, Vector2Int centerTile)
    {
        if (ctx == null || boarOasisStampDefinition == null)
            return null;

        IReadOnlyList<SiteTileLayoutDefinition> variants = boarOasisStampDefinition.TileLayoutVariants;
        int validVariantCount = CountValidLayoutVariants(variants);
        if (validVariantCount <= 0)
            return GetFallbackLayoutDefinition(boarOasisStampDefinition);

        if (validVariantCount == 1)
            return GetValidLayoutVariantAt(variants, 0) ?? GetFallbackLayoutDefinition(boarOasisStampDefinition);

        uint variantHash = DeterministicHash.Hash(
            (uint)ctx.ActiveBiome.Seed,
            centerTile.x,
            centerTile.y,
            LayoutVariantSelectionSalt);
        int variantIndex = Mathf.Min(
            validVariantCount - 1,
            Mathf.FloorToInt(DeterministicHash.Hash01(variantHash) * validVariantCount));

        return GetValidLayoutVariantAt(variants, variantIndex) ?? GetFallbackLayoutDefinition(boarOasisStampDefinition);
    }

    private static SiteTileLayoutDefinition GetFallbackLayoutDefinition(SiteStampDefinition stampDefinition)
    {
        if (stampDefinition == null)
            return null;

        return HasPaintedLayout(stampDefinition.TileLayoutDefinition)
            ? stampDefinition.TileLayoutDefinition
            : null;
    }

    private static int CountValidLayoutVariants(IReadOnlyList<SiteTileLayoutDefinition> variants)
    {
        if (variants == null)
            return 0;

        int count = 0;
        for (int i = 0; i < variants.Count; i++)
        {
            if (HasPaintedLayout(variants[i]))
                count++;
        }

        return count;
    }

    private static SiteTileLayoutDefinition GetValidLayoutVariantAt(
        IReadOnlyList<SiteTileLayoutDefinition> variants,
        int validIndex)
    {
        if (variants == null || validIndex < 0)
            return null;

        int currentValidIndex = 0;
        for (int i = 0; i < variants.Count; i++)
        {
            SiteTileLayoutDefinition variant = variants[i];
            if (!HasPaintedLayout(variant))
                continue;

            if (currentValidIndex == validIndex)
                return variant;

            currentValidIndex++;
        }

        return null;
    }

    private static bool HasStampContent(SiteStampDefinition stampDefinition)
    {
        if (stampDefinition == null)
            return false;

        if (stampDefinition.HasGroundStamp
            || stampDefinition.HasNavigationBlockerStamp
            || stampDefinition.HasVisualClearZone)
        {
            return true;
        }

        if (HasPaintedLayout(stampDefinition.TileLayoutDefinition))
            return true;

        IReadOnlyList<SiteTileLayoutDefinition> variants = stampDefinition.TileLayoutVariants;
        if (variants == null)
            return false;

        for (int i = 0; i < variants.Count; i++)
        {
            if (HasPaintedLayout(variants[i]))
                return true;
        }

        return false;
    }

    private static bool HasPaintedLayout(SiteTileLayoutDefinition layoutDefinition)
    {
        if (layoutDefinition == null)
            return false;

        IReadOnlyList<SiteTileLayoutCell> cells = layoutDefinition.Cells;
        if (cells == null)
            return false;

        for (int i = 0; i < cells.Count; i++)
        {
            if (HasPaintedTile(cells[i]))
                return true;
        }

        return false;
    }

    private static bool HasPaintedTile(SiteTileLayoutCell cell)
    {
        return cell.ground != null
               || cell.water != null
               || cell.decoration != null
               || cell.obstacle != null
               || cell.canopy != null;
    }

    private TileResolver GetTileResolver()
    {
        if (tileResolver == null)
            tileResolver = new TileResolver();

        return tileResolver;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        minSiteCount = Mathf.Max(0, minSiteCount);
        maxSiteCount = Mathf.Max(minSiteCount, maxSiteCount);
        minSpacingTiles = Mathf.Max(1, minSpacingTiles);
        placementRadiusFactor = Mathf.Clamp(placementRadiusFactor, MinPlacementRadiusFactor, 1f);
        avoidOriginRadiusTiles = Mathf.Max(0, avoidOriginRadiusTiles);
        roadSpacingTiles = Mathf.Max(0, roadSpacingTiles);
    }
#endif
}
