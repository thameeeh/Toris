using System.Collections.Generic;
using UnityEngine;

public readonly struct StreamingDiagnosticsSnapshot
{
    public readonly IReadOnlyCollection<Vector2Int> LoadedChunks;
    public readonly int LoadedChunkCount;
    public readonly int GenerationQueueCount;
    public readonly int QueuedChunkCount;
    public readonly int PreloadChunks;
    public readonly int UnloadHysteresisChunks;
    public readonly bool HasStreamingBounds;
    public readonly ChunkStreamingBounds StreamingBounds;
    public readonly bool StreamingAnchorInitialized;
    public readonly Vector2Int StreamingAnchorChunk;
    public readonly int ChunkSize;

    public StreamingDiagnosticsSnapshot(
        IReadOnlyCollection<Vector2Int> loadedChunks,
        int loadedChunkCount,
        int generationQueueCount,
        int queuedChunkCount,
        int preloadChunks,
        int unloadHysteresisChunks,
        bool hasStreamingBounds,
        ChunkStreamingBounds streamingBounds,
        bool streamingAnchorInitialized,
        Vector2Int streamingAnchorChunk,
        int chunkSize)
    {
        LoadedChunks = loadedChunks;
        LoadedChunkCount = loadedChunkCount;
        GenerationQueueCount = generationQueueCount;
        QueuedChunkCount = queuedChunkCount;
        PreloadChunks = preloadChunks;
        UnloadHysteresisChunks = unloadHysteresisChunks;
        HasStreamingBounds = hasStreamingBounds;
        StreamingBounds = streamingBounds;
        StreamingAnchorInitialized = streamingAnchorInitialized;
        StreamingAnchorChunk = streamingAnchorChunk;
        ChunkSize = chunkSize;
    }
}
