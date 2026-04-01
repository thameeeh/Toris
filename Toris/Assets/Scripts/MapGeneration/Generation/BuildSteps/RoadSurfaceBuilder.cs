using UnityEngine;

public static class RoadSurfaceBuilder
{
    private enum Direction
    {
        North,
        East,
        South,
        West
    }

    private const int PlatformWidth = 9;
    private const int PlatformHeight = 9;
    private const int InitialRoadOffsetTiles = 6;
    private const uint RoadHashSalt = 5001u;
    private const int MinAllowedRoadWidth = 3;
    private const int MaxAllowedRoadWidth = 5;
    private const int MinimumRoadScanTiles = 128;

    public static void Build(WorldContext ctx)
    {
        StampPlatform(ctx);

        BuildRoad(ctx, Direction.North);
        BuildRoad(ctx, Direction.East);
        BuildRoad(ctx, Direction.South);
        BuildRoad(ctx, Direction.West);
    }

    private static void StampPlatform(WorldContext ctx)
    {
        if (ctx.Biome == null || ctx.Biome.platformGroundTile == null)
            return;

        ctx.BuildOutput.TerrainOverrides.StampRectGround(
            ctx.ActiveBiome.OriginTile,
            PlatformWidth,
            PlatformHeight,
            ctx.Biome.platformGroundTile);
    }

    private static void BuildRoad(WorldContext ctx, Direction direction)
    {
        if (ctx.Biome == null)
            return;

        Vector2Int originTile = ctx.ActiveBiome.OriginTile;
        Vector2Int stepDirection = Step(direction);
        Vector2Int perpendicularDirection = Perpendicular(direction);

        Vector2Int currentTile = originTile + stepDirection * InitialRoadOffsetTiles;
        Vector2Int lastLandTile = currentTile;

        int maxRoadScanTiles = Mathf.Max(MinimumRoadScanTiles, ctx.Biome.maxRoadScanTiles);

        for (int i = 0; i < maxRoadScanTiles; i++)
        {
            Vector2Int localTile = ctx.ActiveBiome.ToLocal(currentTile);
            bool isLand = ctx.Mask.IsLand(localTile, ctx);
            if (!isLand)
                break;

            lastLandTile = currentTile;
            StampRoadAt(ctx, currentTile, perpendicularDirection);

            currentTile += stepDirection;
        }

        ctx.BuildOutput.RoadAnchors.AddGateAnchor(lastLandTile);
    }

    private static void StampRoadAt(WorldContext ctx, Vector2Int centerTile, Vector2Int perpendicularDirection)
    {
        if (ctx.Biome == null || ctx.Biome.roadTile == null)
            return;

        uint roadHash = DeterministicHash.Hash(
            (uint)ctx.ActiveBiome.Seed,
            centerTile.x,
            centerTile.y,
            RoadHashSalt);

        float widthSample = DeterministicHash.Hash01(roadHash);

        int minimumRoadWidth = Mathf.Clamp(ctx.Biome.roadWidthMin, MinAllowedRoadWidth, MaxAllowedRoadWidth);
        int maximumRoadWidth = Mathf.Clamp(ctx.Biome.roadWidthMax, MinAllowedRoadWidth, MaxAllowedRoadWidth);
        if (maximumRoadWidth < minimumRoadWidth)
            maximumRoadWidth = minimumRoadWidth;

        int roadWidth = minimumRoadWidth + Mathf.FloorToInt(widthSample * (maximumRoadWidth - minimumRoadWidth + 1));
        roadWidth = Mathf.Clamp(roadWidth, MinAllowedRoadWidth, MaxAllowedRoadWidth);

        int halfWidth = roadWidth / 2;

        for (int i = -halfWidth; i <= halfWidth; i++)
        {
            ctx.BuildOutput.TerrainOverrides.SetGround(
                centerTile + perpendicularDirection * i,
                ctx.Biome.roadTile);
        }
    }

    private static Vector2Int Step(Direction direction)
    {
        switch (direction)
        {
            case Direction.North: return new Vector2Int(0, 1);
            case Direction.South: return new Vector2Int(0, -1);
            case Direction.East: return new Vector2Int(1, 0);
            case Direction.West: return new Vector2Int(-1, 0);
            default: return Vector2Int.right;
        }
    }

    private static Vector2Int Perpendicular(Direction direction)
    {
        switch (direction)
        {
            case Direction.North:
            case Direction.South:
                return new Vector2Int(1, 0);

            case Direction.East:
            case Direction.West:
                return new Vector2Int(0, 1);

            default:
                return Vector2Int.up;
        }
    }
}
