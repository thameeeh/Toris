using System.Collections.Generic;
using UnityEngine;

public sealed class DenRegistry
{
    private readonly HashSet<Vector2Int> centers = new HashSet<Vector2Int>();
    private readonly HashSet<Vector2Int> footprint = new HashSet<Vector2Int>();

    public IEnumerable<Vector2Int> DenCenters => centers;

    public void Clear()
    {
        centers.Clear();
        footprint.Clear();
    }

    public void AddDenFootprint(Vector2Int center, int size)
    {
        centers.Add(center);

        int half = Mathf.Max(0, size / 2);
        for (int y = -half; y <= half; y++)
            for (int x = -half; x <= half; x++)
                footprint.Add(center + new Vector2Int(x, y));
    }

    public bool IsDenTile(Vector2Int tile) => footprint.Contains(tile);
}
