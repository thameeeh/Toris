using UnityEngine;

public sealed class BiomeMask
{
    /// <summary>True = land, False = water </summary>
    public bool IsLand(Vector2Int localTile, WorldContext ctx)
    {
        var p = ctx.Biome;
        if (p == null) return true;

        float d = localTile.magnitude;
        float dist01 = d / Mathf.Max(1f, ctx.ActiveBiome.RadiusTiles);

        float n = ctx.Noise.Sample01(NoiseChannel.Coast, localTile.x, localTile.y, p.coastlineNoiseScale);
        float coast = (n - 0.5f) * 2f * p.coastlineNoiseStrength01;

        return (dist01 - coast) < p.landRadius01;
    }
}
