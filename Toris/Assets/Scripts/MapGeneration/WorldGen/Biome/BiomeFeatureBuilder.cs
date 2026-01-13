using UnityEngine;

//TODO: condition for water crossing lakes
public static class BiomeFeatureBuilder
{
    public enum Dir { North, East, South, West }

    public static void Build(WorldContext ctx)
    {
        StampPlatform(ctx);

        BuildRoadAndGate(ctx, Dir.North);
        BuildRoadAndGate(ctx, Dir.East);
        BuildRoadAndGate(ctx, Dir.South);
        BuildRoadAndGate(ctx, Dir.West);
    }

    private static void StampPlatform(WorldContext ctx)
    {
        if (ctx.Biome == null || ctx.Biome.platformGroundTile == null) return;
        ctx.Stamps.StampRectGround(ctx.ActiveBiome.OriginTile, 9, 9, ctx.Biome.platformGroundTile);
    }

    private static Vector2Int Step(Dir d) => d switch
    {
        Dir.North => new Vector2Int(0, 1),
        Dir.South => new Vector2Int(0, -1),
        Dir.East => new Vector2Int(1, 0),
        Dir.West => new Vector2Int(-1, 0),
        _ => Vector2Int.right
    };

    private static Vector2Int Perp(Dir d) => d switch
    {
        Dir.North => new Vector2Int(1, 0),
        Dir.South => new Vector2Int(1, 0),
        Dir.East => new Vector2Int(0, 1),
        Dir.West => new Vector2Int(0, 1),
        _ => Vector2Int.up
    };

    private static void BuildRoadAndGate(WorldContext ctx, Dir dir)
    {
        if (ctx.Biome == null) return;

        Vector2Int origin = ctx.ActiveBiome.OriginTile;
        Vector2Int step = Step(dir);
        Vector2Int perp = Perp(dir);

        Vector2Int p = origin + step * 6;

        Vector2Int lastLand = p;
        int max = Mathf.Max(128, ctx.Biome.maxRoadScanTiles);

        for (int i = 0; i < max; i++)
        {
            Vector2Int local = ctx.ActiveBiome.ToLocal(p);
            bool land = ctx.Mask.IsLand(local, ctx);
            if (!land) break;

            lastLand = p;
            StampRoadAt(ctx, p, perp);

            p += step;
        }

        StampGate(ctx, lastLand);
    }

    private static void StampRoadAt(WorldContext ctx, Vector2Int center, Vector2Int perp)
    {
        if (ctx.Biome == null || ctx.Biome.roadTile == null) return;

        uint h = DeterministicHash.Hash((uint)ctx.ActiveBiome.Seed, center.x, center.y, 5001);
        float r = DeterministicHash.Hash01(h);

        int minW = Mathf.Clamp(ctx.Biome.roadWidthMin, 3, 5);
        int maxW = Mathf.Clamp(ctx.Biome.roadWidthMax, 3, 5);
        if (maxW < minW) maxW = minW;

        int width = minW + Mathf.FloorToInt(r * (maxW - minW + 1));
        width = Mathf.Clamp(width, 3, 5);

        int half = width / 2;
        for (int i = -half; i <= half; i++)
            ctx.Stamps.SetGround(center + perp * i, ctx.Biome.roadTile);
    }

    private static void StampGate(WorldContext ctx, Vector2Int gateCenter)
    {
        if (ctx.Biome == null) return;

        int s = Mathf.Max(1, ctx.Biome.gateSize);
        if (ctx.Biome.gateGroundTile != null)
            ctx.Stamps.StampRectGround(gateCenter, s, s, ctx.Biome.gateGroundTile);

        ctx.Gates.AddGateFootprint(gateCenter, s);
    }
}
