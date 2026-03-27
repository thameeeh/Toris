using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public sealed class FeatureStamps
{
    private readonly Dictionary<Vector2Int, TileResult> overrides = new Dictionary<Vector2Int, TileResult>(8192);

    public void Clear() => overrides.Clear();

    public bool TryGet(Vector2Int worldTile, out TileResult result)
        => overrides.TryGetValue(worldTile, out result);

    public void SetGround(Vector2Int worldTile, TileBase ground)
    {
        overrides.TryGetValue(worldTile, out TileResult tr);
        tr.ground = ground;
        overrides[worldTile] = tr;
    }

    public void SetWater(Vector2Int worldTile, TileBase water)
    {
        overrides.TryGetValue(worldTile, out TileResult tr);
        tr.water = water;
        overrides[worldTile] = tr;
    }

    public void SetDecor(Vector2Int worldTile, TileBase decor)
    {
        overrides.TryGetValue(worldTile, out TileResult tr);
        tr.decor = decor;
        overrides[worldTile] = tr;
    }

    public void StampRectGround(Vector2Int center, int w, int h, TileBase ground)
    {
        int hx = w / 2;
        int hy = h / 2;
        for (int y = -hy; y <= hy; y++)
            for (int x = -hx; x <= hx; x++)
                SetGround(center + new Vector2Int(x, y), ground);
    }
}
