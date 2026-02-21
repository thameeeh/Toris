using UnityEngine;
using UnityEngine.Tilemaps;

// TODO: support for multiple decor layers

public sealed class TileResolver
{
    public WorldSignalSampler sampler = new WorldSignalSampler();

    public TileResult Resolve(Vector2Int tilePos, WorldContext ctx)
    {
        // 0) Guaranteed stamps
        if (ctx.Stamps.TryGet(tilePos, out TileResult stamped))
            return stamped;

        // 0.5) Biome-specific override
        if (ctx.ActiveDef != null && ctx.ActiveDef.TryResolveTile(tilePos, ctx, out TileResult custom))
            return custom;

        // 1) Biome mask
        Vector2Int local = ctx.ActiveBiome.ToLocal(tilePos);
        if (!ctx.Mask.IsLand(local, ctx))
            return new TileResult { water = ctx.Biome?.waterTile };

        // 2) Signals
        WorldSignals s = sampler.Compute(tilePos, ctx);

        // 3) Lakes
        if (ctx.Biome != null && s.lake01 >= ctx.Biome.lakeThreshold01)
            return new TileResult { water = ctx.Biome.waterTile };

        // 4) Base ground
        TileResult r = new TileResult
        {
            ground = PickGround(local, ctx)
        };

        // 5) Decor layers (trees first, then flowers)
        if (TryPickTreeDecor(local, ctx, s, out TileBase tree))
        {
            r.decor = tree;
            return r;
        }

        if (TryPickFlowerDecor(local, ctx, s, out TileBase flower))
            r.decor = flower;

        return r;
    }

    private TileBase PickGround(Vector2Int local, WorldContext ctx)
    {
        var variants = ctx.Biome?.groundVariants;
        if (variants == null || variants.Length == 0) return null;

        uint h = DeterministicHash.Hash((uint)ctx.ActiveBiome.Seed, local.x, local.y, 101);
        int idx = (int)(DeterministicHash.Hash01(h) * variants.Length);
        idx = Mathf.Clamp(idx, 0, variants.Length - 1);
        return variants[idx];
    }

    private bool TryPickTreeDecor(Vector2Int local, WorldContext ctx, WorldSignals s, out TileBase tile)
    {
        tile = null;
        var bp = ctx.Biome;
        if (bp == null) return false;

        if (bp.vegetationDecorVariants == null || bp.vegetationDecorVariants.Length == 0) return false;

        float prob = Mathf.Clamp01(Mathf.Lerp(0f, bp.vegetationMaxProb, s.vegetation01));
        if (prob <= 0f) return false;

        uint h = DeterministicHash.Hash((uint)ctx.ActiveBiome.Seed, local.x, local.y, 202);
        if (DeterministicHash.Hash01(h) > prob) return false;

        int idx = (int)(DeterministicHash.Hash01(h ^ 0xA5A5u) * bp.vegetationDecorVariants.Length);
        idx = Mathf.Clamp(idx, 0, bp.vegetationDecorVariants.Length - 1);
        tile = bp.vegetationDecorVariants[idx];
        return true;
    }

    private bool TryPickFlowerDecor(Vector2Int local, WorldContext ctx, WorldSignals s, out TileBase tile)
    {
        tile = null;
        var bp = ctx.Biome;
        if (bp == null) return false;

        if (bp.flowerDecorVariants == null || bp.flowerDecorVariants.Length == 0) return false;

        float prob = bp.flowerBaseProb * (1f - s.vegetation01);
        if (prob <= 0f) return false;

        uint h = DeterministicHash.Hash((uint)ctx.ActiveBiome.Seed, local.x, local.y, 303);
        if (DeterministicHash.Hash01(h) > prob) return false;

        int idx = (int)(DeterministicHash.Hash01(h ^ 0x5AA5u) * bp.flowerDecorVariants.Length);
        idx = Mathf.Clamp(idx, 0, bp.flowerDecorVariants.Length - 1);
        tile = bp.flowerDecorVariants[idx];
        return true;
    }
}
