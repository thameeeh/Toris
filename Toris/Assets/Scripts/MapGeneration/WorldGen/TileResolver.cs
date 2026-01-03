using UnityEngine;
using UnityEngine.Tilemaps;

public sealed class TileResolver
{
    public WorldSignalSampler sampler = new WorldSignalSampler();

    public TileResult Resolve(Vector2Int tilePos, WorldContext ctx)
    {
        WorldSignals s = sampler.Compute(tilePos, ctx);

        // 1) Island edge: water outside
        if (s.islandMask01 <= 0f)
        {
            return new TileResult { water = ctx.Profile.waterTile };
        }

        // 2) Lakes (only on land)
        bool isLake = s.lake01 >= ctx.Profile.lakeThreshold01;
        if (isLake)
        {
            return new TileResult { water = ctx.Profile.waterTile };
        }

        // 3) Roads override ground (on land only)
        if (ctx.Profile.enableRoads && ctx.Profile.roadTile != null && s.road01 > 0.5f)
        {
            return new TileResult { ground = ctx.Profile.roadTile };
        }

        // 4) Base plains ground
        TileResult r = new TileResult
        {
            ground = PickPlainsGround(tilePos, ctx, s)
        };

        bool nearRoad = s.road01 > 0.25f;
        if (nearRoad)
            return r; // no decor on/near roads

        // 5) Forest overlay decor (Biome 2)
        if (TryPickForestDecor(tilePos, ctx, s, out TileBase forestDecor))
        {
            r.decor = forestDecor;
            return r;
        }

        // 6) Flowers (Plains decor) – suppressed by forest
        if (TryPickFlowerDecor(tilePos, ctx, s, out TileBase flower))
        {
            r.decor = flower;
        }

        return r;
    }

    private TileBase PickPlainsGround(Vector2Int p, WorldContext ctx, WorldSignals s)
    {
        TileBase[] variants = ctx.Profile.plainsGroundVariants;
        if (variants == null || variants.Length == 0) return null;

        // Deterministic pick using hash
        uint h = DeterministicHash.Hash((uint)ctx.Seed, p.x, p.y, 101);
        int idx = (int)(DeterministicHash.Hash01(h) * variants.Length);
        if (idx < 0) idx = 0;
        if (idx >= variants.Length) idx = variants.Length - 1;
        return variants[idx];
    }

    private bool TryPickForestDecor(Vector2Int p, WorldContext ctx, WorldSignals s, out TileBase tile)
    {
        tile = null;
        if (ctx.Profile.treeDecorVariants == null || ctx.Profile.treeDecorVariants.Length == 0) return false;

        // Probability rises with forest01 (tunable feel)
        float prob = Mathf.Clamp01(Mathf.Lerp(0f, 0.75f, s.forest01));
        if (prob <= 0f) return false;

        uint h = DeterministicHash.Hash((uint)ctx.Seed, p.x, p.y, 202);
        if (DeterministicHash.Hash01(h) > prob) return false;

        int idx = (int)(DeterministicHash.Hash01(h ^ 0xA5A5u) * ctx.Profile.treeDecorVariants.Length);
        idx = Mathf.Clamp(idx, 0, ctx.Profile.treeDecorVariants.Length - 1);
        tile = ctx.Profile.treeDecorVariants[idx];
        return true;
    }

    private bool TryPickFlowerDecor(Vector2Int p, WorldContext ctx, WorldSignals s, out TileBase tile)
    {
        tile = null;
        if (ctx.Profile.flowerDecorVariants == null || ctx.Profile.flowerDecorVariants.Length == 0) return false;

        // Flowers are common in plains, but fade out as forest grows
        float baseProb = 0.10f;
        float prob = baseProb * (1f - s.forest01);
        if (prob <= 0f) return false;

        uint h = DeterministicHash.Hash((uint)ctx.Seed, p.x, p.y, 303);
        if (DeterministicHash.Hash01(h) > prob) return false;

        int idx = (int)(DeterministicHash.Hash01(h ^ 0x5AA5u) * ctx.Profile.flowerDecorVariants.Length);
        idx = Mathf.Clamp(idx, 0, ctx.Profile.flowerDecorVariants.Length - 1);
        tile = ctx.Profile.flowerDecorVariants[idx];
        return true;
    }

    private static float SmoothStep(float a, float b, float t)
    {
        if (Mathf.Approximately(a, b)) return t >= b ? 1f : 0f;
        t = Mathf.Clamp01((t - a) / (b - a));
        return t * t * (3f - 2f * t);
    }
}
