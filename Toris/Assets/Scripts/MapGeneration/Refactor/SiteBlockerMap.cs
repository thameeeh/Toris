using System.Collections.Generic;
using UnityEngine;

public sealed class SiteBlockerMap
{
    private readonly HashSet<Vector2Int> blockedTiles = new HashSet<Vector2Int>();

    public void Clear()
    {
        blockedTiles.Clear();
    }

    public void AddSquareFootprint(Vector2Int centerTile, int size)
    {
        int clampedSize = Mathf.Max(1, size);
        int half = Mathf.Max(0, clampedSize / 2);

        for (int y = -half; y <= half; y++)
        {
            for (int x = -half; x <= half; x++)
            {
                blockedTiles.Add(centerTile + new Vector2Int(x, y));
            }
        }
    }

    public bool IsBlocked(Vector2Int tile)
    {
        return blockedTiles.Contains(tile);
    }
}