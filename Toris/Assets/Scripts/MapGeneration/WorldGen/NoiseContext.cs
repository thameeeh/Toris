using UnityEngine;

public enum NoiseChannel { Coast, ForestRegion, Variation, Lake }

public sealed class NoiseContext
{
    private readonly Vector2 coastOffset;
    private readonly Vector2 forestOffset;
    private readonly Vector2 variationOffset;
    private readonly Vector2 lakeOffset;

    public NoiseContext(int seed)
    {
        // stable offsets derived from seed (not UnityEngine.Random)
        coastOffset = MakeOffset(seed, 11);
        forestOffset = MakeOffset(seed, 22);
        variationOffset = MakeOffset(seed, 33);
        lakeOffset = MakeOffset(seed, 44);
    }

    public float Sample01(NoiseChannel channel, float x, float y, float scale)
    {
        Vector2 o = channel switch
        {
            NoiseChannel.Coast => coastOffset,
            NoiseChannel.ForestRegion => forestOffset,
            NoiseChannel.Variation => variationOffset,
            NoiseChannel.Lake => lakeOffset,
            _ => Vector2.zero
        };

        // Mathf.PerlinNoise returns 0..1
        return Mathf.PerlinNoise((x + o.x) * scale, (y + o.y) * scale);
    }

    private static Vector2 MakeOffset(int seed, uint salt)
    {
        uint h1 = DeterministicHash.Hash((uint)seed, 12345, 67890, salt);
        uint h2 = DeterministicHash.Hash((uint)seed, -2222, 9999, salt + 1);
        float ox = DeterministicHash.Hash01(h1) * 10000f;
        float oy = DeterministicHash.Hash01(h2) * 10000f;
        return new Vector2(ox, oy);
    }
}
