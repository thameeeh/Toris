using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    menuName = "WorldGen/Biomes/Site Rules/Necromancer Grave Site Rule",
    fileName = "NecromancerGraveSitePlacementRuleDefinition")]
public sealed class NecromancerGraveSitePlacementRuleDefinition : SitePlacementRuleDefinition
{
    private const uint GravePickSalt = 0xC0DE1234u;
    private const uint GraveCountSalt = 0xC0DECAFEu;
    private const uint TrailDirectionSalt = 0xA11CE551u;
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
    [SerializeField, Min(0)] private int graveMinDistanceFromMainRoadTiles = 10;

    [Header("Approach Trail")]
    [SerializeField] private bool stampApproachTrail = true;
    [SerializeField, Min(0)] private int trailStopDistanceFromGraveTiles = 4;
    [SerializeField, Min(0)] private int trailStartHalfWidth = 1;
    [SerializeField, Min(0)] private int trailEndHalfWidth = 0;
    [SerializeField, Min(1)] private int trailRoadFollowTiles = 8;

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
                return ctx.Mask.IsLand(localTile, ctx)
                    && IsFarEnoughFromMainRoad(candidateTile, originTile);
            });

        for (int i = 0; i < chosenCenters.Count; i++)
        {
            Vector2Int centerTile = chosenCenters[i];
            TryStampApproachTrail(ctx, centerTile);
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

    private void TryStampApproachTrail(WorldContext ctx, Vector2Int graveCenterTile)
    {
        if (!stampApproachTrail || ctx?.Biome?.roadTile == null)
            return;

        Vector2Int originTile = ctx.ActiveBiome.OriginTile;
        Vector2 startPoint;
        Vector2 controlPoint;
        Vector2 endPoint = graveCenterTile;

        bool useVerticalRoad = Mathf.Abs(graveCenterTile.x - originTile.x) <= Mathf.Abs(graveCenterTile.y - originTile.y);
        if (useVerticalRoad)
        {
            int roadDirectionY = ResolveRoadDirection(graveCenterTile.y - originTile.y, ctx.ActiveBiome.Seed, graveCenterTile);
            startPoint = new Vector2(originTile.x, graveCenterTile.y - roadDirectionY * trailRoadFollowTiles);
            controlPoint = startPoint + Vector2.up * roadDirectionY * trailRoadFollowTiles;
        }
        else
        {
            int roadDirectionX = ResolveRoadDirection(graveCenterTile.x - originTile.x, ctx.ActiveBiome.Seed, graveCenterTile);
            startPoint = new Vector2(graveCenterTile.x - roadDirectionX * trailRoadFollowTiles, originTile.y);
            controlPoint = startPoint + Vector2.right * roadDirectionX * trailRoadFollowTiles;
        }

        float straightLineDistance = Vector2.Distance(startPoint, endPoint);
        if (straightLineDistance <= 0.001f)
            return;

        float endProgress = Mathf.Clamp01(
            (straightLineDistance - Mathf.Max(0, trailStopDistanceFromGraveTiles)) / straightLineDistance);
        if (endProgress <= 0f)
            return;

        int sampleCount = Mathf.Max(2, Mathf.CeilToInt(straightLineDistance * 2f));
        Vector2Int lastStampedTile = new Vector2Int(int.MinValue, int.MinValue);
        Vector2 tangentAtStart = controlPoint - startPoint;
        Vector2Int perpendicular = Mathf.Abs(tangentAtStart.x) > Mathf.Abs(tangentAtStart.y)
            ? Vector2Int.up
            : Vector2Int.right;

        for (int i = 0; i <= sampleCount; i++)
        {
            float progress = i / (float)sampleCount;
            float t = progress * endProgress;
            Vector2 curvePoint = EvaluateQuadraticBezier(startPoint, controlPoint, endPoint, t);
            Vector2Int trailTile = new Vector2Int(
                Mathf.RoundToInt(curvePoint.x),
                Mathf.RoundToInt(curvePoint.y));
            if (trailTile == lastStampedTile)
                continue;

            int halfWidth = Mathf.RoundToInt(Mathf.Lerp(trailStartHalfWidth, trailEndHalfWidth, progress));
            StampTrailSegment(ctx, trailTile, perpendicular, Mathf.Max(0, halfWidth));
            lastStampedTile = trailTile;
        }
    }

    private void StampTrailSegment(WorldContext ctx, Vector2Int centerTile, Vector2Int perpendicular, int halfWidth)
    {
        if (ctx?.BuildOutput == null || ctx.Biome?.roadTile == null)
            return;

        for (int i = -halfWidth; i <= halfWidth; i++)
            ctx.BuildOutput.TerrainOverrides.SetGround(centerTile + perpendicular * i, ctx.Biome.roadTile);
    }

    private bool IsFarEnoughFromMainRoad(Vector2Int candidateTile, Vector2Int originTile)
    {
        int minDistanceFromRoad = Mathf.Max(0, graveMinDistanceFromMainRoadTiles);
        if (minDistanceFromRoad <= 0)
            return true;

        int distanceToVerticalRoad = Mathf.Abs(candidateTile.x - originTile.x);
        int distanceToHorizontalRoad = Mathf.Abs(candidateTile.y - originTile.y);
        int distanceToNearestRoad = Mathf.Min(distanceToVerticalRoad, distanceToHorizontalRoad);
        return distanceToNearestRoad >= minDistanceFromRoad;
    }

    private int ResolveRoadDirection(int signedDelta, int biomeSeed, Vector2Int graveCenterTile)
    {
        if (signedDelta != 0)
            return signedDelta > 0 ? 1 : -1;

        uint directionHash = DeterministicHash.Hash(
            (uint)biomeSeed,
            graveCenterTile.x,
            graveCenterTile.y,
            TrailDirectionSalt);
        return DeterministicHash.Hash01(directionHash) < 0.5f ? -1 : 1;
    }

    private static Vector2 EvaluateQuadraticBezier(Vector2 start, Vector2 control, Vector2 end, float t)
    {
        float oneMinusT = 1f - Mathf.Clamp01(t);
        return oneMinusT * oneMinusT * start
            + 2f * oneMinusT * t * control
            + t * t * end;
    }
}
