using System;
using UnityEngine;

public readonly struct ChunkStreamingFrameSettings
{
    public readonly int PreloadChunks;
    public readonly int UnloadHysteresisChunks;
    public readonly int MaxChunksPerFrame;
    public readonly int MaxUnloadRemovalsPerFrame;
    public readonly double GenerationBudgetMs;

    public ChunkStreamingFrameSettings(
        int preloadChunks,
        int unloadHysteresisChunks,
        double generationBudgetMs,
        int maxChunksPerFrame,
        int maxUnloadRemovalsPerFrame)
    {
        PreloadChunks = Mathf.Max(0, preloadChunks);
        UnloadHysteresisChunks = Mathf.Max(0, unloadHysteresisChunks);
        MaxChunksPerFrame = Mathf.Max(0, maxChunksPerFrame);
        MaxUnloadRemovalsPerFrame = Mathf.Max(0, maxUnloadRemovalsPerFrame);
        GenerationBudgetMs = Math.Max(0.1, generationBudgetMs);
    }
}
