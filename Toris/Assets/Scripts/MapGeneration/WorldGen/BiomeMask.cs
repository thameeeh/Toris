using UnityEngine;

public sealed class BiomeMask
{
    private readonly WorldGenProfile profile;

    public BiomeMask(WorldGenProfile profile)
    {
        this.profile = profile;
    }

    /// <summary>True = land, False = water </summary>
    public bool IsLand(Vector2Int localTile, WorldContext ctx)
    {
        float d = localTile.magnitude;
        float dist01 = d / Mathf.Max(1f, ctx.ActiveBiome.RadiusTiles);

        float n = ctx.Noise.Sample01(NoiseChannel.Coast, localTile.x, localTile.y, profile.coastNoiseScale);
        float coast = (n - 0.5f) * 2f * profile.coastNoiseStrength01;

        float cutoff = profile.islandRadius01;

        return (dist01 - coast) < cutoff;
    }
}