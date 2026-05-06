using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    menuName = "WorldGen/Biomes/Site Rules/Roadside Vignette Rule",
    fileName = "RoadsideVignettePlacementRuleDefinition")]
public sealed class RoadsideVignettePlacementRuleDefinition : SitePlacementRuleDefinition
{
    private enum AuthoredRoadDirection
    {
        Down,
        Right,
        Up,
        Left
    }

    private enum PlacementMode
    {
        FeatureCount,
        FillAvailableRoadside
    }

    private const uint VignettePickSalt = 0xA120BEEFu;
    private const uint VignetteCountSalt = 0xA120C001u;
    private const uint LayoutVariantSelectionSalt = 0xA120CAFEu;
    private const uint RoadDirectionSelectionSalt = 0xA120D1ECu;

    private static readonly Vector2Int[] CardinalDirections =
    {
        Vector2Int.up,
        Vector2Int.right,
        Vector2Int.down,
        Vector2Int.left
    };

    [System.NonSerialized] private WorldSignalSampler sampler;

    [Header("Layouts")]
    [Tooltip("Cell-space direction from the origin toward the road in the authored layout. This is grid direction, not screen direction.")]
    [SerializeField] private AuthoredRoadDirection authoredRoadDirection = AuthoredRoadDirection.Down;
    [SerializeField] private List<SiteTileLayoutDefinition> roadsideLayoutVariants = new();

    [Header("Mode")]
    [SerializeField] private PlacementMode placementMode = PlacementMode.FeatureCount;

    [Header("Count")]
    [SerializeField, Min(0)] private int minVignetteCount = 4;
    [SerializeField, Min(0)] private int maxVignetteCount = 8;

    [Header("Placement")]
    [SerializeField, Min(1)] private int roadsideMinSpacingTiles = 18;
    [SerializeField, Range(0.1f, 1f)] private float placementRadiusFactor = 0.9f;
    [SerializeField, Min(0)] private int avoidOriginRadiusTiles = 16;
    [SerializeField, Range(0f, 1f)] private float fillChance = 0.6f;
    [SerializeField] private bool avoidExistingStamps = true;

    public override void BuildSites(WorldContext ctx)
    {
        if (ctx == null || ctx.Biome == null || ctx.BuildOutput == null)
            return;

        if (!HasAnyRoadTile(ctx.Biome) || CountValidLayouts() <= 0)
            return;

        List<RoadsideCandidate> candidates = CollectRoadsideCandidates(ctx);
        int spacingTiles = Mathf.Max(1, roadsideMinSpacingTiles);
        List<RoadsideCandidate> chosenCenters = placementMode == PlacementMode.FillAvailableRoadside
            ? PickFillCandidates(candidates, spacingTiles, fillChance)
            : PickFeatureCandidates(candidates, ResolveTargetCount(ctx.ActiveBiome.Seed), spacingTiles);

        for (int i = 0; i < chosenCenters.Count; i++)
        {
            RoadsideCandidate candidate = chosenCenters[i];
            SiteTileLayoutDefinition layoutDefinition = ResolveLayoutDefinition(ctx, candidate.CenterTile);

            StampLayoutClippedToRoadsideLand(
                ctx,
                candidate.CenterTile,
                layoutDefinition,
                candidate.RoadDirection);
        }

#if UNITY_EDITOR
        int targetCount = placementMode == PlacementMode.FillAvailableRoadside
            ? 0
            : ResolveTargetCount(ctx.ActiveBiome.Seed);
        if (targetCount > 0 && chosenCenters.Count < targetCount)
        {
            Debug.LogWarning(
                $"[RoadsideVignetteRule] Only placed {chosenCenters.Count}/{targetCount} roadside vignettes (road edges too constrained).",
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

    private List<RoadsideCandidate> CollectRoadsideCandidates(WorldContext ctx)
    {
        int radiusTiles = Mathf.CeilToInt(ctx.ActiveBiome.RadiusTiles * Mathf.Clamp01(placementRadiusFactor));
        int radiusTilesSquared = radiusTiles * radiusTiles;
        int avoidOriginRadius = Mathf.Max(0, avoidOriginRadiusTiles);
        int avoidOriginRadiusSquared = avoidOriginRadius * avoidOriginRadius;
        Vector2Int originTile = ctx.ActiveBiome.OriginTile;

        List<RoadsideCandidate> candidates = new List<RoadsideCandidate>();

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
                if (!TryGetRoadDirection(ctx, candidateTile, out Vector2Int roadDirection))
                    continue;

                SiteTileLayoutDefinition layoutDefinition = ResolveLayoutDefinition(ctx, candidateTile);
                if (!HasStampableLandCell(ctx, candidateTile, layoutDefinition, roadDirection))
                    continue;

                uint pickHash = DeterministicHash.Hash(
                    (uint)ctx.ActiveBiome.Seed,
                    candidateTile.x,
                    candidateTile.y,
                    VignettePickSalt);

                candidates.Add(new RoadsideCandidate(candidateTile, roadDirection, pickHash));
            }
        }

        candidates.Sort(CompareCandidates);
        return candidates;
    }

    private static List<RoadsideCandidate> PickFeatureCandidates(
        List<RoadsideCandidate> candidates,
        int targetCount,
        int spacingTiles)
    {
        List<RoadsideCandidate> chosenCandidates = new List<RoadsideCandidate>(Mathf.Max(0, targetCount));
        if (candidates == null || targetCount <= 0)
            return chosenCandidates;

        int spacingTilesSquared = spacingTiles * spacingTiles;
        for (int i = 0; i < candidates.Count && chosenCandidates.Count < targetCount; i++)
        {
            RoadsideCandidate candidate = candidates[i];
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

    private static List<RoadsideCandidate> PickFillCandidates(
        List<RoadsideCandidate> candidates,
        int spacingTiles,
        float chance)
    {
        List<RoadsideCandidate> chosenCandidates = new List<RoadsideCandidate>();
        if (candidates == null || candidates.Count == 0)
            return chosenCandidates;

        int spacingTilesSquared = spacingTiles * spacingTiles;
        float resolvedChance = Mathf.Clamp01(chance);

        for (int i = 0; i < candidates.Count; i++)
        {
            RoadsideCandidate candidate = candidates[i];
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

    private static int CompareCandidates(RoadsideCandidate a, RoadsideCandidate b)
    {
        int hashComparison = a.SortHash.CompareTo(b.SortHash);
        if (hashComparison != 0)
            return hashComparison;

        int xComparison = a.CenterTile.x.CompareTo(b.CenterTile.x);
        if (xComparison != 0)
            return xComparison;

        return a.CenterTile.y.CompareTo(b.CenterTile.y);
    }

    private bool TryGetRoadDirection(
        WorldContext ctx,
        Vector2Int landTile,
        out Vector2Int roadDirection)
    {
        roadDirection = Vector2Int.zero;

        if (!IsGeneratedRoadsideLandTile(ctx, landTile))
            return false;

        int roadNeighborCount = 0;
        for (int i = 0; i < CardinalDirections.Length; i++)
        {
            if (IsRoadTile(ctx, landTile + CardinalDirections[i]))
                roadNeighborCount++;
        }

        if (roadNeighborCount <= 0)
            return false;

        uint directionHash = DeterministicHash.Hash(
            (uint)ctx.ActiveBiome.Seed,
            landTile.x,
            landTile.y,
            RoadDirectionSelectionSalt);
        int selectedRoadNeighbor = Mathf.Min(
            roadNeighborCount - 1,
            Mathf.FloorToInt(DeterministicHash.Hash01(directionHash) * roadNeighborCount));

        int currentRoadNeighbor = 0;
        for (int i = 0; i < CardinalDirections.Length; i++)
        {
            Vector2Int direction = CardinalDirections[i];
            if (!IsRoadTile(ctx, landTile + direction))
                continue;

            if (currentRoadNeighbor == selectedRoadNeighbor)
            {
                roadDirection = direction;
                return true;
            }

            currentRoadNeighbor++;
        }

        return false;
    }

    private bool HasStampableLandCell(
        WorldContext ctx,
        Vector2Int centerTile,
        SiteTileLayoutDefinition layoutDefinition,
        Vector2Int roadDirection)
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

            Vector2Int worldTile = centerTile + TransformOffsetTowardRoad(cell.offset, roadDirection);
            if (IsGeneratedRoadsideLandTile(ctx, worldTile) && !HasBlockedExistingStamp(ctx, worldTile))
                return true;
        }

        return false;
    }

    private void StampLayoutClippedToRoadsideLand(
        WorldContext ctx,
        Vector2Int centerTile,
        SiteTileLayoutDefinition layoutDefinition,
        Vector2Int roadDirection)
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
            Vector2Int worldTile = centerTile + TransformOffsetTowardRoad(cell.offset, roadDirection);

            if (!HasLandSideVisual(cell) || !IsGeneratedRoadsideLandTile(ctx, worldTile))
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
        if (roadsideLayoutVariants == null)
            return 0;

        int count = 0;
        for (int i = 0; i < roadsideLayoutVariants.Count; i++)
        {
            if (roadsideLayoutVariants[i] != null)
                count++;
        }

        return count;
    }

    private SiteTileLayoutDefinition GetValidLayoutAt(int validIndex)
    {
        if (roadsideLayoutVariants == null || validIndex < 0)
            return null;

        int currentValidIndex = 0;
        for (int i = 0; i < roadsideLayoutVariants.Count; i++)
        {
            SiteTileLayoutDefinition layoutDefinition = roadsideLayoutVariants[i];
            if (layoutDefinition == null)
                continue;

            if (currentValidIndex == validIndex)
                return layoutDefinition;

            currentValidIndex++;
        }

        return null;
    }

    private bool IsGeneratedRoadsideLandTile(WorldContext ctx, Vector2Int worldTile)
    {
        Vector2Int localTile = ctx.ActiveBiome.ToLocal(worldTile);
        return ctx.Mask.IsLand(localTile, ctx)
            && !IsGeneratedLakeWaterTile(ctx, worldTile)
            && !IsRoadTile(ctx, worldTile);
    }

    private bool IsGeneratedLakeWaterTile(WorldContext ctx, Vector2Int worldTile)
    {
        Vector2Int localTile = ctx.ActiveBiome.ToLocal(worldTile);
        if (!ctx.Mask.IsLand(localTile, ctx))
            return false;

        WorldSignals signals = GetSampler().Compute(worldTile, ctx);
        return signals.lake01 >= ctx.Biome.lakeThreshold01;
    }

    private bool IsRoadTile(WorldContext ctx, Vector2Int worldTile)
    {
        if (ctx?.BuildOutput?.TerrainOverrides == null)
            return false;

        if (!ctx.BuildOutput.TerrainOverrides.TryGet(worldTile, out TileResult tileResult))
            return false;

        return IsRoadGroundTile(ctx.Biome, tileResult.ground);
    }

    private static bool IsRoadGroundTile(BiomeProfile biome, UnityEngine.Tilemaps.TileBase groundTile)
    {
        if (biome == null || groundTile == null)
            return false;

        if (groundTile == biome.roadTile)
            return true;

        UnityEngine.Tilemaps.TileBase[] variants = biome.roadVariantTiles;
        if (variants == null)
            return false;

        for (int i = 0; i < variants.Length; i++)
        {
            if (groundTile == variants[i])
                return true;
        }

        return false;
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

    private static bool HasAnyRoadTile(BiomeProfile biome)
    {
        if (biome == null)
            return false;

        if (biome.roadTile != null)
            return true;

        UnityEngine.Tilemaps.TileBase[] variants = biome.roadVariantTiles;
        if (variants == null)
            return false;

        for (int i = 0; i < variants.Length; i++)
        {
            if (variants[i] != null)
                return true;
        }

        return false;
    }

    private static bool HasLandSideVisual(SiteTileLayoutCell cell)
    {
        return cell.ground != null
            || cell.decoration != null
            || cell.obstacle != null
            || cell.canopy != null;
    }

    private Vector2Int TransformOffsetTowardRoad(Vector2Int offset, Vector2Int targetRoadDirection)
    {
        return SiteStamping.TransformOffsetTowardWaterDirection(
            offset,
            AuthoredRoadDirectionToVector(authoredRoadDirection),
            targetRoadDirection);
    }

    private static Vector2Int AuthoredRoadDirectionToVector(AuthoredRoadDirection direction)
    {
        switch (direction)
        {
            case AuthoredRoadDirection.Right:
                return Vector2Int.right;

            case AuthoredRoadDirection.Up:
                return Vector2Int.up;

            case AuthoredRoadDirection.Left:
                return Vector2Int.left;

            default:
                return Vector2Int.down;
        }
    }

    private readonly struct RoadsideCandidate
    {
        public readonly Vector2Int CenterTile;
        public readonly Vector2Int RoadDirection;
        public readonly uint SortHash;

        public RoadsideCandidate(Vector2Int centerTile, Vector2Int roadDirection, uint sortHash)
        {
            CenterTile = centerTile;
            RoadDirection = roadDirection;
            SortHash = sortHash;
        }
    }
}
