using System.Collections.Generic;
using UnityEngine;

public sealed class SitePlacementIndex
{
    private readonly List<SitePlacement> _all = new();
    private readonly Dictionary<Vector2Int, List<SitePlacement>> _byChunk = new();

    public IReadOnlyList<SitePlacement> All => _all;

    public void Clear()
    {
        _all.Clear();
        _byChunk.Clear();
    }

    public void Add(in SitePlacement placement)
    {
        _all.Add(placement);

        if(!_byChunk.TryGetValue(placement.ChunkCoord, out var list))
        {
            list = new List<SitePlacement>(4);
            _byChunk.Add(placement.ChunkCoord, list);
        }

        list.Add(placement);
    }

    public bool TryGetChunk(Vector2Int chunkCoord, out List<SitePlacement> placements)
        => _byChunk.TryGetValue(chunkCoord, out placements);
}