using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    menuName = "Outland Haven/WorldGen/Biomes/Site Rules/Shoreline Vignette Rule",
    fileName = "ShorelineVignettePlacementRuleDefinition")]
public sealed class ShorelineVignettePlacementRuleDefinition : SitePlacementRuleDefinition
{
    private enum AuthoredWaterDirection
    {
        Down,
        Right,
        Up,
        Left
    }

    private enum PlacementMode
    {
        FeatureCount,
        FillAvailableShoreline
    }

    private const uint VignettePickSalt = 0x5A10BEEFu;
    private const uint VignetteCountSalt = 0x5A10C001u;
    private const uint LayoutVariantSelectionSalt = 0x5A10CAFEu;
    private const uint WaterDirectionSelectionSalt = 0x5A10D1ECu;

    private static readonly Vector2Int[] CardinalDirections =
    {
        Vector2Int.up,
        Vector2Int.right,
        Vector2Int.down,
        Vector2Int.left
    };

    [System.NonSerialized] private WorldSignalSampler sampler;

    [Header("Layouts")]
    [Tooltip("Cell-space direction from the origin toward water in the authored layout. This is grid direction, not screen direction.")]
    [SerializeField] private AuthoredWaterDirection authoredWaterDirection = AuthoredWaterDirection.Down;
    [SerializeField] private List<SiteTileLayoutDefinition> shorelineLayoutVariants = new();

    [Header("Mode")]
    [SerializeField] private PlacementMode placementMode = PlacementMode.FeatureCount;

    [Header("Count")]
    [SerializeField, Min(0)] private int minVignetteCount = 2;
    [SerializeField, Min(0)] private int maxVignetteCount = 5;

    [Header("Placement")]
    [SerializeField, Min(1)] private int shorelineMinSpacingTiles = 28;
    [SerializeField, Range(0.1f, 1f)] private float placementRadiusFactor = 0.9f;
    [SerializeField, Min(0)] private int avoidOriginRadiusTiles = 24;
    [SerializeField, Range(0f, 1f)] private float fillChance = 1f;
    [SerializeField] private bool avoidExistingStamps = true;

    public override void BuildSites(WorldContext ctx)
    {
        if (ctx == null || ctx.Biome == null || ctx.Biome.waterTile == null || ctx.BuildOutput == null)
            return;

        int validLayoutCount = CountValidLayouts();
        if (validLayoutCount <= 0)
            return;

        List<ShorelineCandidate> candidates = CollectShorelineCandidates(ctx);
        int spacingTiles = Mathf.Max(1, shorelineMinSpacingTiles);
        List<ShorelineCandidate> chosenCenters = placementMode == PlacementMode.FillAvailableShoreline
            ? PickFillCandidates(candidates, spacingTiles, fillChance)
            : PickFeatureCandidates(candidates, ResolveTargetCount(ctx.ActiveBiome.Seed), spacingTiles);

        for (int i = 0; i < chosenCenters.Count; i++)
        {
            ShorelineCandidate candidate = chosenCenters[i];
            SiteTileLayoutDefinition layoutDefinition = ResolveLayoutDefinition(ctx, candidate.CenterTile);

            StampLayoutClippedToGeneratedLand(
                ctx,
                candidate.CenterTile,
                layoutDefinition,
                candidate.WaterDirection);
        }

#if UNITY_EDITOR
        int targetCount = placementMode == PlacementMode.FillAvailableShoreline
            ? 0
            : ResolveTargetCount(ctx.ActiveBiome.Seed);
        if (targetCount > 0 && chosenCenters.Count < targetCount)
        {
            Debug.LogWarning(
                $"[ShorelineVignetteRule] Only placed {chosenCenters.Count}/{targetCount} shoreline vignettes (lake edges too constrained).",
                this);
        }
#endif
    }

    private int ResolveTargetCount(int biomeSeed)
    {
        int resolvedMin = Mathf.Max(0, minVignetteCount);
        int resolvedMax = Mathf.Max(resolvedMin, maxVignetteCount);
        if (resolvedMax == resolvedMin)
            return resolvedMin;

        uint countHash = DeterministicHash.Hash((uint)biomeSeed, resolvedMin, resolvedMax, VignetteCountSalt);
        int range = resolvedMax - resolvedMin + 1;
        int offset = Mathf.FloorToInt(DeterministicHash.Hash01(countHash) * range);
        return Mathf.Clamp(resolvedMin + offset, resolvedMin, resolvedMax);
    }

    private List<ShorelineCandidate> CollectShorelineCandidates(WorldContext ctx)
    {
        int radiusTiles = Mathf.CeilToInt(ctx.ActiveBiome.RadiusTiles * Mathf.Clamp01(placementRadiusFactor));
        int radiusTilesSquared = radiusTiles * radiusTiles;
        int avoidOriginRadius = Mathf.Max(0, avoidOriginRadiusTiles);
        int avoidOriginRadiusSquared = avoidOriginRadius * avoidOriginRadius;
        Vector2Int originTile = ctx.ActiveBiome.OriginTile;

        List<ShorelineCandidate> candidates = new List<ShorelineCandidate>();

        for (int y = -radiusTiles; y <= radiusTiles; y++)
        {
            for (int x = -radiusTiles; x <= radiusTiles; x++)
            {
                Vector2Int offset = new Vector2Int(x, y);
                int distanceSquared = offset.sqrMagnitude;
                if (distanceSquared > radiusTilesSquared)
                    continue;

                if (avoidOriginRadiusSquared > 0 && distanceSquared < avoidOriginRadiusSquared)
                    continue;

                Vector2Int candidateTile = originTile + offset;
                if (!TryGetLakeWaterDirection(ctx, candidateTile, out Vector2Int waterDirection))
                    continue;

                SiteTileLayoutDefinition layoutDefinition = ResolveLayoutDefinition(ctx, candidateTile);
                if (!HasStampableLandCell(ctx, candidateTile, layoutDefinition, waterDirection))
                    continue;

                uint pickHash = DeterministicHash.Hash(
                    (uint)ctx.ActiveBiome.Seed,
                    candidateTile.x,
                    candidateTile.y,
                    VignettePickSalt);

                candidates.Add(new ShorelineCandidate(candidateTile, waterDirection, pickHash));
            }
        }

        candidates.Sort(CompareCandidates);
        return candidates;
    }

    private static List<ShorelineCandidate> PickFeatureCandidates(
        List<ShorelineCandidate> candidates,
        int targetCount,
        int spacingTiles)
    {
        List<ShorelineCandidate> chosenCandidates = new List<ShorelineCandidate>(Mathf.Max(0, targetCount));
        if (candidates == null || targetCount <= 0)
            return chosenCandidates;

        int spacingTilesSquared = spacingTiles * spacingTiles;
        for (int i = 0; i < candidates.Count && chosenCandidates.Count < targetCount; i++)
        {
            ShorelineCandidate candidate = candidates[i];
            bool isFarEnough = true;

            for (int j = 0; j < chosenCandidates.Count; j++)
            {
                if ((chosenCandidates[j].CenterTile - candidate.CenterTile).sqrMagnitude < spacingTilesSquared)
                {
                    isFarEnough = false;
                    break;
                }
            }

            if (isFarEnough)
                chosenCandidates.Add(candidate);
        }

        return chosenCandidates;
    }

    private static List<ShorelineCandidate> PickFillCandidates(
        List<ShorelineCandidate> candidates,
        int spacingTiles,
        float chance)
    {
        List<ShorelineCandidate> chosenCandidates = new List<ShorelineCandidate>();
        if (candidates == null || candidates.Count == 0)
            return chosenCandidates;

        int spacingTilesSquared = spacingTiles * spacingTiles;
        float resolvedChance = Mathf.Clamp01(chance);

        for (int i = 0; i < candidates.Count; i++)
        {
            ShorelineCandidate candidate = candidates[i];
            if (resolvedChance <= 0f || DeterministicHash.Hash01(candidate.SortHash) > resolvedChance)
                continue;

            bool isFarEnough = true;
            for (int j = 0; j < chosenCandidates.Count; j++)
            {
                if ((chosenCandidates[j].CenterTile - candidate.CenterTile).sqrMagnitude < spacingTilesSquared)
                {
                    isFarEnough = false;
                    break;
                }
            }

            if (isFarEnough)
                chosenCandidates.Add(candidate);
        }

        return chosenCandidates;
    }

    private static int CompareCandidates(ShorelineCandidate a, ShorelineCandidate b)
    {
        int hashComparison = a.SortHash.CompareTo(b.SortHash);
        if (hashComparison != 0)
            return hashComparison;

        int xComparison = a.CenterTile.x.CompareTo(b.CenterTile.x);
        if (xComparison != 0)
            return xComparison;

        return a.CenterTile.y.CompareTo(b.CenterTile.y);
    }

    private bool TryGetLakeWaterDirection(
        WorldContext ctx,
        Vector2Int landTile,
        out Vector2Int waterDirection)
    {
        waterDirection = Vector2Int.zero;

        if (!IsGeneratedLandTile(ctx, landTile))
            return false;

        int waterNeighborCount = 0;
        for (int i = 0; i < CardinalDirections.Length; i++)
        {
            if (IsGeneratedLakeWaterTile(ctx, landTile + CardinalDirections[i]))
                waterNeighborCount++;
        }

        if (waterNeighborCount <= 0)
            return false;

        uint directionHash = DeterministicHash.Hash(
            (uint)ctx.ActiveBiome.Seed,
            landTile.x,
            landTile.y,
            WaterDirectionSelectionSalt);
        int selectedWaterNeighbor = Mathf.Min(
            waterNeighborCount - 1,
            Mathf.FloorToInt(DeterministicHash.Hash01(directionHash) * waterNeighborCount));

        int currentWaterNeighbor = 0;
        for (int i = 0; i < CardinalDirections.Length; i++)
        {
            Vector2Int direction = CardinalDirections[i];
            if (!IsGeneratedLakeWaterTile(ctx, landTile + direction))
                continue;

            if (currentWaterNeighbor == selectedWaterNeighbor)
            {
                waterDirection = direction;
                return true;
            }

            currentWaterNeighbor++;
        }

        return false;
    }

    private bool HasStampableLandCell(
        WorldContext ctx,
        Vector2Int centerTile,
        SiteTileLayoutDefinition layoutDefinition,
        Vector2Int waterDirection)
    {
        if (layoutDefinition == null)
            return false;

        IReadOnlyList<SiteTileLayoutCell> cells = layoutDefinition.Cells;
        if (cells == null || cells.Count == 0)
            return false;

        for (int i = 0; i < cells.Count; i++)
        {
            SiteTileLayoutCell cell = cells[i];
            if (!HasLandSideVisual(cell))
                continue;

            Vector2Int worldTile = centerTile + TransformOffsetTowardWater(cell.offset, waterDirection);
            if (IsGeneratedLandTile(ctx, worldTile) && !HasBlockedExistingStamp(ctx, worldTile))
                return true;
        }

        return false;
    }

    private void StampLayoutClippedToGeneratedLand(
        WorldContext ctx,
        Vector2Int centerTile,
        SiteTileLayoutDefinition layoutDefinition,
        Vector2Int waterDirection)
    {
        if (ctx?.BuildOutput == null || layoutDefinition == null)
            return;

        FeatureStamps terrainOverrides = ctx.BuildOutput.TerrainOverrides;
        IReadOnlyList<SiteTileLayoutCell> cells = layoutDefinition.Cells;
        if (terrainOverrides == null || cells == null)
            return;

        for (int i = 0; i < cells.Count; i++)
        {
            SiteTileLayoutCell cell = cells[i];
            Vector2Int worldTile = centerTile + TransformOffsetTowardWater(cell.offset, waterDirection);

            if (cell.water != null && IsGeneratedLakeWaterTile(ctx, worldTile))
                terrainOverrides.SetWater(worldTile, cell.water);

            if (!HasLandSideVisual(cell) || !IsGeneratedLandTile(ctx, worldTile))
                continue;

            if (HasBlockedExistingStamp(ctx, worldTile))
                continue;

            if (cell.ground != null)
                terrainOverrides.SetGround(worldTile, cell.ground);

            if (cell.decoration != null)
                terrainOverrides.SetDecoration(worldTile, cell.decoration);

            if (cell.obstacle != null)
                terrainOverrides.SetObstacle(worldTile, cell.obstacle);

            if (cell.canopy != null)
                terrainOverrides.SetCanopy(worldTile, cell.canopy);
        }
    }

    private SiteTileLayoutDefinition ResolveLayoutDefinition(WorldContext ctx, Vector2Int centerTile)
    {
        int validLayoutCount = CountValidLayouts();
        if (validLayoutCount <= 0)
            return null;

        if (validLayoutCount == 1)
            return GetValidLayoutAt(0);

        uint variantHash = DeterministicHash.Hash(
            (uint)ctx.ActiveBiome.Seed,
            centerTile.x,
            centerTile.y,
            LayoutVariantSelectionSalt);
        int variantIndex = Mathf.Min(
            validLayoutCount - 1,
            Mathf.FloorToInt(DeterministicHash.Hash01(variantHash) * validLayoutCount));

        return GetValidLayoutAt(variantIndex);
    }

    private int CountValidLayouts()
    {
        if (shorelineLayoutVariants == null)
            return 0;

        int count = 0;
        for (int i = 0; i < shorelineLayoutVariants.Count; i++)
        {
            if (shorelineLayoutVariants[i] != null)
                count++;
        }

        return count;
    }

    private SiteTileLayoutDefinition GetValidLayoutAt(int validIndex)
    {
        if (shorelineLayoutVariants == null || validIndex < 0)
            return null;

        int currentValidIndex = 0;
        for (int i = 0; i < shorelineLayoutVariants.Count; i++)
        {
            SiteTileLayoutDefinition layoutDefinition = shorelineLayoutVariants[i];
            if (layoutDefinition == null)
                continue;

            if (currentValidIndex == validIndex)
                return layoutDefinition;

            currentValidIndex++;
        }

        return null;
    }

    private bool IsGeneratedLandTile(WorldContext ctx, Vector2Int worldTile)
    {
        Vector2Int localTile = ctx.ActiveBiome.ToLocal(worldTile);
        return ctx.Mask.IsLand(localTile, ctx) && !IsGeneratedLakeWaterTile(ctx, worldTile);
    }

    private bool IsGeneratedLakeWaterTile(WorldContext ctx, Vector2Int worldTile)
    {
        Vector2Int localTile = ctx.ActiveBiome.ToLocal(worldTile);
        if (!ctx.Mask.IsLand(localTile, ctx))
            return false;

        WorldSignals signals = GetSampler().Compute(worldTile, ctx);
        return signals.lake01 >= ctx.Biome.lakeThreshold01;
    }

    private bool HasBlockedExistingStamp(WorldContext ctx, Vector2Int worldTile)
    {
        if (!avoidExistingStamps || ctx?.BuildOutput?.TerrainOverrides == null)
            return false;

        return ctx.BuildOutput.TerrainOverrides.TryGet(worldTile, out TileResult existingStamp);
    }

    private WorldSignalSampler GetSampler()
    {
        if (sampler == null)
            sampler = new WorldSignalSampler();

        return sampler;
    }

    private static bool HasLandSideVisual(SiteTileLayoutCell cell)
    {
        return cell.ground != null
            || cell.decoration != null
            || cell.obstacle != null
            || cell.canopy != null;
    }

    private Vector2Int TransformOffsetTowardWater(Vector2Int offset, Vector2Int targetWaterDirection)
    {
        return SiteStamping.TransformOffsetTowardWaterDirection(
            offset,
            AuthoredWaterDirectionToVector(authoredWaterDirection),
            targetWaterDirection);
    }

    private static Vector2Int AuthoredWaterDirectionToVector(AuthoredWaterDirection direction)
    {
        switch (direction)
        {
            case AuthoredWaterDirection.Right:
                return Vector2Int.right;

            case AuthoredWaterDirection.Up:
                return Vector2Int.up;

            case AuthoredWaterDirection.Left:
                return Vector2Int.left;

            default:
                return Vector2Int.down;
        }
    }

    private readonly struct ShorelineCandidate
    {
        public readonly Vector2Int CenterTile;
        public readonly Vector2Int WaterDirection;
        public readonly uint SortHash;

        public ShorelineCandidate(Vector2Int centerTile, Vector2Int waterDirection, uint sortHash)
        {
            CenterTile = centerTile;
            WaterDirection = waterDirection;
            SortHash = sortHash;
        }
    }
}
