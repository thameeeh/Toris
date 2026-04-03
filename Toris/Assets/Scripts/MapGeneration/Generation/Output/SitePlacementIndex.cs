using System.Collections.Generic;
using UnityEngine;

public sealed class SitePlacementIndex
{
    private readonly List<SitePlacement> _all = new();
    private readonly Dictionary<Vector2Int, List<SitePlacement>> _chunkScopedByChunk = new();
    private readonly List<SitePlacement> _persistentBiomePlacements = new();

    public IReadOnlyList<SitePlacement> All => _all;
    public IReadOnlyList<SitePlacement> PersistentBiomePlacements => _persistentBiomePlacements;
    public int ChunkPlacementCount => _all.Count - _persistentBiomePlacements.Count;
    public int PersistentPlacementCount => _persistentBiomePlacements.Count;

    public void Clear()
    {
        _all.Clear();
        _chunkScopedByChunk.Clear();
        _persistentBiomePlacements.Clear();
    }

    public void Add(in SitePlacement placement)
    {
        _all.Add(placement);

        if (placement.LifecycleScope == SitePlacementLifecycleScope.PersistentBiome)
        {
            _persistentBiomePlacements.Add(placement);
            return;
        }

        if(!_chunkScopedByChunk.TryGetValue(placement.ChunkCoord, out var list))
        {
            list = new List<SitePlacement>(4);
            _chunkScopedByChunk.Add(placement.ChunkCoord, list);
        }

        list.Add(placement);
    }

    public bool TryGetChunk(Vector2Int chunkCoord, out List<SitePlacement> placements)
        => _chunkScopedByChunk.TryGetValue(chunkCoord, out placements);
}
