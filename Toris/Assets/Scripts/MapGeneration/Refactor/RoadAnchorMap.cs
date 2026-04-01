using System.Collections.Generic;
using UnityEngine;

public sealed class RoadAnchorMap
{
    private readonly List<Vector2Int> gateAnchorTiles = new List<Vector2Int>();

    public IReadOnlyList<Vector2Int> GateAnchorTiles => gateAnchorTiles;
    public int GateAnchorCount => gateAnchorTiles.Count;

    public void Clear()
    {
        gateAnchorTiles.Clear();
    }

    public void AddGateAnchor(Vector2Int gateAnchorTile)
    {
        gateAnchorTiles.Add(gateAnchorTile);
    }
}
