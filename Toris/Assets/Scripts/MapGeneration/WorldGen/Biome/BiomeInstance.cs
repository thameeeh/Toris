using UnityEngine;

public readonly struct BiomeInstance
{
    public readonly int Index;
    public readonly int Seed;
    public readonly Vector2Int OriginTile;
    public readonly float RadiusTiles;

    public BiomeInstance(int index, int seed, Vector2Int originTile, float radiusTiles)
    {
        Index = index;
        Seed = seed;
        OriginTile = originTile;
        RadiusTiles = Mathf.Max(1f, radiusTiles);
    }

    public Vector2Int ToLocal(Vector2Int worldTile) => worldTile - OriginTile;
}
