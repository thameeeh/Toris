using System.Collections.Generic;
using UnityEngine;

public sealed class GateRegistry
{
    private readonly HashSet<Vector2Int> tiles = new HashSet<Vector2Int>();
    private readonly HashSet<Vector2Int> centers = new HashSet<Vector2Int>();

    public void Clear()
    {
        tiles.Clear();
        centers.Clear();
    }

    public void AddGateFootprint(Vector2Int gateCenterWorld, int size)
    {
        centers.Add(gateCenterWorld);

        int h = size / 2;
        for (int y = -h; y <= h; y++)
            for (int x = -h; x <= h; x++)
                tiles.Add(gateCenterWorld + new Vector2Int(x, y));
    }

    public bool IsGateTile(Vector2Int worldTile) => tiles.Contains(worldTile);
    public IEnumerable<Vector2Int> GateCenters => centers;
}
