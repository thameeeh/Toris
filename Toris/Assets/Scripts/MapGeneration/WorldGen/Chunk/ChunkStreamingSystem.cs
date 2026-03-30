using System.Collections.Generic;
using UnityEngine;

public sealed class ChunkStreamingSystem
{
    private readonly Queue<Vector2Int> generationQueue = new Queue<Vector2Int>();
    private readonly HashSet<Vector2Int> queuedChunks = new HashSet<Vector2Int>();
    private readonly HashSet<Vector2Int> loadedChunks = new HashSet<Vector2Int>();

    private Vector2Int streamingAnchorChunk;
    private bool anchorInitialized;

    public IReadOnlyCollection<Vector2Int> LoadedChunks => loadedChunks;
    public int LoadedChunkCount => loadedChunks.Count;
    public int QueuedChunkCount => queuedChunks.Count;
    public int GenerationQueueCount => generationQueue.Count;

    public Vector2Int StreamingAnchorChunk => streamingAnchorChunk;
    public bool AnchorInitialized => anchorInitialized;

    public void Reset()
    {
        generationQueue.Clear();
        queuedChunks.Clear();
        loadedChunks.Clear();
        streamingAnchorChunk = default;
        anchorInitialized = false;
    }

    public void SetStreamingAnchor(Vector2Int chunkCoord)
    {
        streamingAnchorChunk = chunkCoord;
        anchorInitialized = true;
    }

    public bool IsChunkLoaded(Vector2Int chunkCoord)
    {
        return loadedChunks.Contains(chunkCoord);
    }

    public void MarkChunkLoaded(Vector2Int chunkCoord)
    {
        loadedChunks.Add(chunkCoord);
        queuedChunks.Remove(chunkCoord);
    }

    public void MarkChunkUnloaded(Vector2Int chunkCoord)
    {
        loadedChunks.Remove(chunkCoord);
        queuedChunks.Remove(chunkCoord);
    }

    public void EnqueueNeededChunks(Vector2Int minChunk, Vector2Int maxChunk)
    {
        List<Vector2Int> neededChunks = new List<Vector2Int>();

        Vector2Int centerChunk = new Vector2Int(
            (minChunk.x + maxChunk.x) / 2,
            (minChunk.y + maxChunk.y) / 2
        );

        for (int y = minChunk.y; y <= maxChunk.y; y++)
        {
            for (int x = minChunk.x; x <= maxChunk.x; x++)
            {
                Vector2Int chunkCoord = new Vector2Int(x, y);

                if (loadedChunks.Contains(chunkCoord) || queuedChunks.Contains(chunkCoord))
                    continue;

                neededChunks.Add(chunkCoord);
            }
        }

        neededChunks.Sort((a, b) =>
        {
            int distanceA = Mathf.Abs(a.x - centerChunk.x) + Mathf.Abs(a.y - centerChunk.y);
            int distanceB = Mathf.Abs(b.x - centerChunk.x) + Mathf.Abs(b.y - centerChunk.y);
            return distanceA.CompareTo(distanceB);
        });

        for (int i = 0; i < neededChunks.Count; i++)
        {
            Vector2Int chunkCoord = neededChunks[i];
            generationQueue.Enqueue(chunkCoord);
            queuedChunks.Add(chunkCoord);
        }
    }

    public bool TryDequeueNextChunk(Vector2Int minChunk, Vector2Int maxChunk, out Vector2Int chunkCoord)
    {
        while (generationQueue.Count > 0)
        {
            Vector2Int candidateChunk = generationQueue.Dequeue();
            queuedChunks.Remove(candidateChunk);

            if (loadedChunks.Contains(candidateChunk))
                continue;

            if (!IsChunkIn(candidateChunk, minChunk, maxChunk))
                continue;

            chunkCoord = candidateChunk;
            return true;
        }

        chunkCoord = default;
        return false;
    }

    public List<Vector2Int> CollectChunksToUnload(
        Vector2Int keepMinChunk,
        Vector2Int keepMaxChunk,
        int maxRemovalsPerFrame)
    {
        if (loadedChunks.Count == 0 || maxRemovalsPerFrame <= 0)
            return null;

        List<(Vector2Int chunkCoord, int score)> candidates = null;

        foreach (Vector2Int chunkCoord in loadedChunks)
        {
            if (IsChunkIn(chunkCoord, keepMinChunk, keepMaxChunk))
                continue;

            int dx = 0;
            if (chunkCoord.x < keepMinChunk.x)
                dx = keepMinChunk.x - chunkCoord.x;
            else if (chunkCoord.x > keepMaxChunk.x)
                dx = chunkCoord.x - keepMaxChunk.x;

            int dy = 0;
            if (chunkCoord.y < keepMinChunk.y)
                dy = keepMinChunk.y - chunkCoord.y;
            else if (chunkCoord.y > keepMaxChunk.y)
                dy = chunkCoord.y - keepMaxChunk.y;

            int score = dx + dy;

            if (candidates == null)
                candidates = new List<(Vector2Int, int)>();

            candidates.Add((chunkCoord, score));
        }

        if (candidates == null || candidates.Count == 0)
            return null;

        candidates.Sort((a, b) => b.score.CompareTo(a.score));

        int removalCount = Mathf.Min(maxRemovalsPerFrame, candidates.Count);
        List<Vector2Int> chunksToUnload = new List<Vector2Int>(removalCount);

        for (int i = 0; i < removalCount; i++)
        {
            chunksToUnload.Add(candidates[i].chunkCoord);
        }

        return chunksToUnload;
    }

    private static bool IsChunkIn(Vector2Int chunkCoord, Vector2Int minChunk, Vector2Int maxChunk)
    {
        return chunkCoord.x >= minChunk.x &&
               chunkCoord.x <= maxChunk.x &&
               chunkCoord.y >= minChunk.y &&
               chunkCoord.y <= maxChunk.y;
    }
}
