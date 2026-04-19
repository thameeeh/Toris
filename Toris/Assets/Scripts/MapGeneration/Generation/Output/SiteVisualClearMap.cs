using System.Collections.Generic;
using UnityEngine;

public sealed class SiteVisualClearMap
{
    private readonly HashSet<Vector2Int> clearedTiles = new HashSet<Vector2Int>();

    public int ClearedTileCount => clearedTiles.Count;

    public void Clear()
    {
        clearedTiles.Clear();
    }

    public bool Contains(Vector2Int tile)
    {
        return clearedTiles.Contains(tile);
    }

    public void AddRectFootprint(Vector2Int centerTile, int width, int height)
    {
        int clampedWidth = Mathf.Max(1, width);
        int clampedHeight = Mathf.Max(1, height);
        int halfWidth = Mathf.Max(0, clampedWidth / 2);
        int halfHeight = Mathf.Max(0, clampedHeight / 2);

        for (int y = -halfHeight; y <= halfHeight; y++)
        {
            for (int x = -halfWidth; x <= halfWidth; x++)
            {
                clearedTiles.Add(centerTile + new Vector2Int(x, y));
            }
        }
    }
}
