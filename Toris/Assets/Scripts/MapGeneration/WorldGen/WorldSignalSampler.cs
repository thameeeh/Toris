using UnityEngine;

public sealed class WorldSignalSampler
{
    public WorldSignals Compute(Vector2Int worldTile, WorldContext ctx)
    {
        WorldSignals s = new WorldSignals();

        // biome-local distance (0..1)
        Vector2Int local = ctx.ActiveBiome.ToLocal(worldTile);
        float d = local.magnitude;
        s.dist01 = Mathf.Clamp01(d / Mathf.Max(1f, ctx.ActiveBiome.RadiusTiles));
        s.danger01 = Mathf.Clamp01(ctx.DangerCurve.Evaluate(s.dist01));

        // all noise sampled in LOCAL space so new biome genuinely feels regenerated
        s.variation01 = ctx.Noise.Sample01(NoiseChannel.Variation, local.x, local.y, 0.03f);
        s.lake01 = ctx.Noise.Sample01(NoiseChannel.Lake, local.x, local.y, ctx.Profile.lakeScale);

        float forestDist01 = SmoothStep(ctx.Profile.forestStart01, ctx.Profile.forestFull01, s.dist01);
        float region = ctx.Noise.Sample01(NoiseChannel.ForestRegion, local.x, local.y, ctx.Profile.forestRegionScale);
        s.forest01 = Mathf.Clamp01(forestDist01 * (0.6f + 0.8f * region) * ctx.Profile.forestDensityMultiplier);

        // road01 optional; we can treat stamps as “road presence” instead
        s.road01 = 0f;

        return s;
    }

    private static float SmoothStep(float a, float b, float x)
    {
        float t = Mathf.InverseLerp(a, b, x);
        return t * t * (3f - 2f * t);
    }
}
