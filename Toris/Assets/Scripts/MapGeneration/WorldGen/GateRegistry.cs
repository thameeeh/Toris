using System.Collections.Generic;
using UnityEngine;

public sealed class GateRegistry
{
    private readonly HashSet<Vector2Int> tiles = new HashSet<Vector2Int>();

    public void Clear() => tiles.Clear();

    public void AddGateFootprint(Vector2Int gateCenterWorld, int size)
    {
        int h = size / 2;
        for (int y = -h; y <= h; y++)
            for (int x = -h; x <= h; x++)
                tiles.Add(gateCenterWorld + new Vector2Int(x, y));
    }

    public bool IsGateTile(Vector2Int worldTile) => tiles.Contains(worldTile);
}
