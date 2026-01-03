using UnityEngine;

public sealed class WorldSignalSampler
{
    // --- instrumentation accumulators (reset each chunk manually) ---
    public long ticksTotal;
    public long ticksRoad;

    public void ResetCounters()
    {
        ticksTotal = 0;
        ticksRoad = 0;
    }

    public WorldSignals Compute(Vector2Int p, WorldContext ctx)
    {
        long t0 = System.Diagnostics.Stopwatch.GetTimestamp();

        WorldSignals s = new WorldSignals();

        float d = Vector2.Distance((Vector2)p, ctx.SpawnPosTiles);
        float dist01Unclamped = d / ctx.WorldRadiusTiles;

        s.dist01 = Mathf.Clamp01(d / ctx.WorldRadiusTiles);
        s.danger01 = Mathf.Clamp01(ctx.DangerCurve.Evaluate(s.dist01));

        s.variation01 = ctx.Noise.Sample01(NoiseChannel.Variation, p.x, p.y, 0.03f);

        if (dist01Unclamped > 1f)
        {
            s.islandMask01 = -1f;
            long t1a = System.Diagnostics.Stopwatch.GetTimestamp();
            ticksTotal += (t1a - t0);
            return s;
        }

        float radial01 = 1f - s.dist01;
        float coastNoise = ctx.Noise.Sample01(NoiseChannel.Coast, p.x, p.y, ctx.Profile.coastNoiseScale);
        float coastDelta = (coastNoise - 0.5f) * 2f * ctx.Profile.coastNoiseStrength01;

        float landness01 = radial01 + coastDelta;
        float cutoff = 1f - ctx.Profile.islandRadius01;
        s.islandMask01 = landness01 - cutoff;

        s.lake01 = ctx.Noise.Sample01(NoiseChannel.Lake, p.x, p.y, ctx.Profile.lakeScale);

        float forestDist01 = SmoothStep(ctx.Profile.forestStart01, ctx.Profile.forestFull01, s.dist01);
        float region = ctx.Noise.Sample01(NoiseChannel.ForestRegion, p.x, p.y, ctx.Profile.forestRegionScale);
        s.forest01 = Mathf.Clamp01(forestDist01 * (0.6f + 0.8f * region) * ctx.Profile.forestDensityMultiplier);

        long tr0 = System.Diagnostics.Stopwatch.GetTimestamp();
        s.road01 = ctx.Roads.ComputeInfluence(p);
        long tr1 = System.Diagnostics.Stopwatch.GetTimestamp();
        ticksRoad += (tr1 - tr0);

        long t1 = System.Diagnostics.Stopwatch.GetTimestamp();
        ticksTotal += (t1 - t0);

        return s;
    }

    private static float SmoothStep(float a, float b, float x)
    {
        float t = Mathf.InverseLerp(a, b, x);
        return t * t * (3f - 2f * t);
    }
}
