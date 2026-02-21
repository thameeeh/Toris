using System.Runtime.CompilerServices;

public static class DeterministicHash
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint Hash(uint seed, int x, int y, uint salt)
    {
        unchecked
        {
            uint h = seed;
            h ^= (uint)x * 0x9E3779B1u;
            h = (h << 16) | (h >> 16);
            h ^= (uint)y * 0x85EBCA6Bu;
            h ^= salt * 0xC2B2AE35u;
            h *= 0x27D4EB2Fu;
            return h;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Hash01(uint h)
    {
        return (h & 0x00FFFFFFu) / 16777216f;
    }
}
