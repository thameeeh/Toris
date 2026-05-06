using System.Collections.Generic;
using UnityEngine;

public sealed class WildlifeSpawnPlacementIndex
{
    private readonly List<WildlifeSpawnPlacement> all = new List<WildlifeSpawnPlacement>();
    private readonly Dictionary<Vector2Int, List<WildlifeSpawnPlacement>> byChunk =
        new Dictionary<Vector2Int, List<WildlifeSpawnPlacement>>();

    public IReadOnlyList<WildlifeSpawnPlacement> All => all;
    public int Count => all.Count;

    public void Clear()
    {
        all.Clear();
        byChunk.Clear();
    }

    public void Add(in WildlifeSpawnPlacement placement)
    {
        all.Add(placement);

        if (!byChunk.TryGetValue(placement.ChunkCoord, out List<WildlifeSpawnPlacement> list))
        {
            list = new List<WildlifeSpawnPlacement>(4);
            byChunk.Add(placement.ChunkCoord, list);
        }

        list.Add(placement);
    }

    public bool TryGetChunk(Vector2Int chunkCoord, out List<WildlifeSpawnPlacement> placements)
    {
        return byChunk.TryGetValue(chunkCoord, out placements);
    }
}
