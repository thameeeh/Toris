using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime-only persistence for a single active biome/run.
/// Stores per-chunk state and provides deterministic IDs for "spawn once".
/// </summary>
public sealed class ChunkStateStore
{
    public sealed class ChunkState
    {
        public bool initialized;
        public readonly HashSet<int> claimedSpawnIds = new HashSet<int>();
        public readonly HashSet<int> consumedIds = new HashSet<int>();
    }

    private readonly Dictionary<Vector2Int, ChunkState> chunkStates = new Dictionary<Vector2Int, ChunkState>();
    public void Clear() => chunkStates.Clear();
    public ChunkState GetChunkState(Vector2Int chunkCoord)
    {
        if (!chunkStates.TryGetValue(chunkCoord, out var st))
        {
            st = new ChunkState();
            chunkStates.Add(chunkCoord, st);
        }
        return st;
    }

    /// <summary>
    /// Stable deterministic ID for a spawn slot in a given chunk.
    /// Use different salts for different spawn systems (enemies vs loot, etc).
    /// </summary>
    public int MakeSpawnId(int biomeSeed, Vector2Int chunkCoord, int localIndex, uint salt)
    {
        unchecked
        {
            uint h = DeterministicHash.Hash((uint)biomeSeed, chunkCoord.x, chunkCoord.y, salt);
            h = DeterministicHash.Hash(h, localIndex, 0, salt ^ 0xA5A5A5A5u);
            return (int)(h & 0x7FFFFFFF);
        }
    }

    /// <summary>
    /// Returns true if this spawn ID was never claimed before in this chunk.
    /// Claim it, then spawn your prefab once.
    /// </summary>
    /// 
    public bool TryClaimSpawn(int biomeSeed, Vector2Int chunkCoord, int localIndex, uint salt, out int spawnId)
    {
        var st = GetChunkState(chunkCoord);
        spawnId = MakeSpawnId(biomeSeed, chunkCoord, localIndex, salt);

        if (st.consumedIds.Contains(spawnId))
            return false;

        return st.claimedSpawnIds.Add(spawnId);
    }

    /// <summary>
    /// Mark something as permanently gone (enemy killed, loot picked, etc).
    /// </summary>
    public void MarkConsumed(Vector2Int chunkCoord, int spawnId)
    {
        var st = GetChunkState(chunkCoord);
        st.consumedIds.Add(spawnId);
    }
}