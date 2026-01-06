using UnityEngine;
using UnityEngine.Tilemaps;

public sealed class TileResolver
{
    public WorldSignalSampler sampler = new WorldSignalSampler();

    public TileResult Resolve(Vector2Int tilePos, WorldContext ctx)
    {
        // 0) Guaranteed stamps (platform/roads/gates/POIs later)
        if (ctx.Stamps.TryGet(tilePos, out TileResult stamped))
            return stamped;

        // 1) Biome mask defines bounded land/water
        Vector2Int local = ctx.ActiveBiome.ToLocal(tilePos);
        if (!ctx.Mask.IsLand(local, ctx))
            return new TileResult { water = ctx.Profile.waterTile };

        // 2) Background noise/signals
        WorldSignals s = sampler.Compute(tilePos, ctx);

        // 3) Lakes (on land only)
        if (s.lake01 >= ctx.Profile.lakeThreshold01)
            return new TileResult { water = ctx.Profile.waterTile };

        // 4) Base plains ground
        TileResult r = new TileResult
        {
            ground = PickPlainsGround(local, ctx)
        };

        // No decor on roads: since roads are stamps, we can cheaply suppress decor
        // by checking whether the ground tile is roadTile in stamps (already handled above).
        // Here we just place decor normally on non-stamped land.

        // Forest decor
        if (TryPickForestDecor(local, ctx, s, out TileBase forestDecor))
        {
            r.decor = forestDecor;
            return r;
        }

        // Flowers
        if (TryPickFlowerDecor(local, ctx, s, out TileBase flower))
            r.decor = flower;

        return r;
    }

    private TileBase PickPlainsGround(Vector2Int local, WorldContext ctx)
    {
        TileBase[] variants = ctx.Profile.plainsGroundVariants;
        if (variants == null || variants.Length == 0) return null;

        uint h = DeterministicHash.Hash((uint)ctx.ActiveBiome.Seed, local.x, local.y, 101);
        int idx = (int)(DeterministicHash.Hash01(h) * variants.Length);
        idx = Mathf.Clamp(idx, 0, variants.Length - 1);
        return variants[idx];
    }

    private bool TryPickForestDecor(Vector2Int local, WorldContext ctx, WorldSignals s, out TileBase tile)
    {
        tile = null;
        if (ctx.Profile.treeDecorVariants == null || ctx.Profile.treeDecorVariants.Length == 0) return false;

        float prob = Mathf.Clamp01(Mathf.Lerp(0f, 0.75f, s.forest01));
        if (prob <= 0f) return false;

        uint h = DeterministicHash.Hash((uint)ctx.ActiveBiome.Seed, local.x, local.y, 202);
        if (DeterministicHash.Hash01(h) > prob) return false;

        int idx = (int)(DeterministicHash.Hash01(h ^ 0xA5A5u) * ctx.Profile.treeDecorVariants.Length);
        idx = Mathf.Clamp(idx, 0, ctx.Profile.treeDecorVariants.Length - 1);
        tile = ctx.Profile.treeDecorVariants[idx];
        return true;
    }

    private bool TryPickFlowerDecor(Vector2Int local, WorldContext ctx, WorldSignals s, out TileBase tile)
    {
        tile = null;
        if (ctx.Profile.flowerDecorVariants == null || ctx.Profile.flowerDecorVariants.Length == 0) return false;

        float baseProb = 0.10f;
        float prob = baseProb * (1f - s.forest01);
        if (prob <= 0f) return false;

        uint h = DeterministicHash.Hash((uint)ctx.ActiveBiome.Seed, local.x, local.y, 303);
        if (DeterministicHash.Hash01(h) > prob) return false;

        int idx = (int)(DeterministicHash.Hash01(h ^ 0x5AA5u) * ctx.Profile.flowerDecorVariants.Length);
        idx = Mathf.Clamp(idx, 0, ctx.Profile.flowerDecorVariants.Length - 1);
        tile = ctx.Profile.flowerDecorVariants[idx];
        return true;
    }
}
