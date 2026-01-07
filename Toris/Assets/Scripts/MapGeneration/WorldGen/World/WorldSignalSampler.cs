using UnityEngine;

public sealed class WorldSignalSampler
{
    public WorldSignals Compute(Vector2Int worldTile, WorldContext ctx)
    {
        var bp = ctx.Biome;

        WorldSignals s = new WorldSignals();

        Vector2Int local = ctx.ActiveBiome.ToLocal(worldTile);
        float d = local.magnitude;
        s.dist01 = Mathf.Clamp01(d / Mathf.Max(1f, ctx.ActiveBiome.RadiusTiles));
        s.danger01 = Mathf.Clamp01(ctx.DangerCurve.Evaluate(s.dist01));

        s.variation01 = ctx.Noise.Sample01(NoiseChannel.Variation, local.x, local.y, 0.03f);

        if (bp == null)
        {
            s.lake01 = 0f;
            s.vegetation01 = 0f;
            s.road01 = 0f;
            return s;
        }

        s.lake01 = ctx.Noise.Sample01(NoiseChannel.Lake, local.x, local.y, bp.lakeNoiseScale);

        float region = ctx.Noise.Sample01(
            NoiseChannel.ForestRegion,
            local.x,
            local.y,
            bp.vegetationRegionScale
        );

        region = Mathf.SmoothStep(0.35f, 0.75f, region);

        s.vegetation01 = Mathf.Clamp01(region * bp.vegetationDensity);

        s.road01 = 0f;
        return s;
    }

    private static float SmoothStep(float a, float b, float x)
    {
        float t = Mathf.InverseLerp(a, b, x);
        return t * t * (3f - 2f * t);
    }
}
