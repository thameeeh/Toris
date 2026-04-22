using UnityEngine;
using UnityEngine.Tilemaps;

public sealed class TileResolver
{
    public WorldSignalSampler sampler = new WorldSignalSampler();

    public TileResult Resolve(Vector2Int tilePos, WorldContext ctx)
    {
        // 0) Guaranteed stamps
        FeatureStamps terrainOverrides = ctx.BuildOutput != null ? ctx.BuildOutput.TerrainOverrides : null;
        if (terrainOverrides != null && terrainOverrides.TryGet(tilePos, out TileResult stamped))
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

        SiteVisualClearMap siteVisualClears = ctx.BuildOutput != null ? ctx.BuildOutput.SiteVisualClears : null;
        if (siteVisualClears != null && siteVisualClears.Contains(tilePos))
            return r;

        // 5) Visual overlays (tree stump/base plus canopy first, then small decoration)
        if (TryPickTreeVariant(local, ctx, s, out TileBase obstacleTile, out TileBase canopyTile))
        {
            r.obstacle = obstacleTile;
            r.canopy = canopyTile;
            return r;
        }

        if (TryPickVegetationDecoration(local, ctx, s, out TileBase vegetationDecoration))
        {
            r.decoration = vegetationDecoration;
            return r;
        }

        if (TryPickFlowerDecoration(local, ctx, s, out TileBase flower))
            r.decoration = flower;

        return r;
    }

    // Unique deterministic salts for different layer permutations
    private const uint HASH_SALT_GROUND = 101;
    private const uint HASH_SALT_TREE = 202;
    private const uint HASH_SALT_TREE_VARIANT = 212;
    private const uint HASH_SALT_SMALL_VEGETATION = 252;
    private const uint HASH_SALT_SMALL_VEGETATION_VARIANT = 262;
    private const uint HASH_SALT_FLOWER = 303;
    private const uint HASH_SALT_FLOWER_VARIANT = 313;

    private TileBase PickGround(Vector2Int local, WorldContext ctx)
    {
        var variants = ctx.Biome?.groundVariants;
        if (variants == null || variants.Length == 0) return null;

        uint h = DeterministicHash.Hash((uint)ctx.ActiveBiome.Seed, local.x, local.y, HASH_SALT_GROUND);
        int idx = (int)(DeterministicHash.Hash01(h) * variants.Length);
        idx = Mathf.Clamp(idx, 0, variants.Length - 1);
        return variants[idx];
    }

    private bool TryPickTreeVariant(
        Vector2Int local,
        WorldContext ctx,
        WorldSignals s,
        out TileBase obstacleTile,
        out TileBase canopyTile)
    {
        obstacleTile = null;
        canopyTile = null;

        var bp = ctx.Biome;
        if (bp == null) return false;

        if (bp.treeVariants == null || bp.treeVariants.Length == 0) return false;

        float prob = Mathf.Clamp01(Mathf.Lerp(0f, bp.treeMaxProb, s.vegetation01));
        if (prob <= 0f) return false;

        uint h = DeterministicHash.Hash((uint)ctx.ActiveBiome.Seed, local.x, local.y, HASH_SALT_TREE);
        if (DeterministicHash.Hash01(h) > prob) return false;

        int variantCount = bp.treeVariants.Length;
        uint variantHash = DeterministicHash.Hash((uint)ctx.ActiveBiome.Seed, local.x, local.y, HASH_SALT_TREE_VARIANT);
        int idx = (int)(DeterministicHash.Hash01(variantHash) * variantCount);
        idx = Mathf.Clamp(idx, 0, variantCount - 1);

        BiomeTreeVariant variant = bp.treeVariants[idx];
        if (variant == null || !variant.IsValid)
            return false;

        obstacleTile = variant.obstacleTile;
        canopyTile = variant.canopyTile;
        return obstacleTile != null || canopyTile != null;
    }

    private bool TryPickVegetationDecoration(Vector2Int local, WorldContext ctx, WorldSignals s, out TileBase tile)
    {
        tile = null;
        var bp = ctx.Biome;
        if (bp == null) return false;

        if (bp.smallVegetationDecorVariants == null || bp.smallVegetationDecorVariants.Length == 0) return false;

        float prob = Mathf.Clamp01(Mathf.Lerp(0f, bp.smallVegetationMaxProb, s.vegetation01));
        if (prob <= 0f) return false;

        uint h = DeterministicHash.Hash((uint)ctx.ActiveBiome.Seed, local.x, local.y, HASH_SALT_SMALL_VEGETATION);
        if (DeterministicHash.Hash01(h) > prob) return false;

        uint variantHash = DeterministicHash.Hash((uint)ctx.ActiveBiome.Seed, local.x, local.y, HASH_SALT_SMALL_VEGETATION_VARIANT);
        int idx = (int)(DeterministicHash.Hash01(variantHash) * bp.smallVegetationDecorVariants.Length);
        idx = Mathf.Clamp(idx, 0, bp.smallVegetationDecorVariants.Length - 1);
        tile = bp.smallVegetationDecorVariants[idx];
        return true;
    }

    private bool TryPickFlowerDecoration(Vector2Int local, WorldContext ctx, WorldSignals s, out TileBase tile)
    {
        tile = null;
        var bp = ctx.Biome;
        if (bp == null) return false;

        if (bp.flowerDecorVariants == null || bp.flowerDecorVariants.Length == 0) return false;

        float prob = bp.flowerBaseProb * (1f - s.vegetation01);
        if (prob <= 0f) return false;

        uint h = DeterministicHash.Hash((uint)ctx.ActiveBiome.Seed, local.x, local.y, HASH_SALT_FLOWER);
        if (DeterministicHash.Hash01(h) > prob) return false;

        uint variantHash = DeterministicHash.Hash((uint)ctx.ActiveBiome.Seed, local.x, local.y, HASH_SALT_FLOWER_VARIANT);
        int idx = (int)(DeterministicHash.Hash01(variantHash) * bp.flowerDecorVariants.Length);
        idx = Mathf.Clamp(idx, 0, bp.flowerDecorVariants.Length - 1);
        tile = bp.flowerDecorVariants[idx];
        return true;
    }
}
