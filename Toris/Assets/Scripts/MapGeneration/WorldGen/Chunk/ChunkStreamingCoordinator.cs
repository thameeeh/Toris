using System;
using UnityEngine;

public sealed class ChunkStreamingCoordinator
{
    private readonly WorldProfile worldProfile;
    private readonly Grid grid;
    private readonly ChunkStreamingSystem chunkStreamingSystem;
    private readonly ChunkProcessingPipeline chunkProcessingPipeline;
    private ChunkStreamingBounds lastBounds;
    private bool hasLastBounds;

    public ChunkStreamingCoordinator(
        WorldProfile worldProfile,
        Grid grid,
        ChunkStreamingSystem chunkStreamingSystem,
        ChunkProcessingPipeline chunkProcessingPipeline)
    {
        this.worldProfile = worldProfile;
        this.grid = grid;
        this.chunkStreamingSystem = chunkStreamingSystem;
        this.chunkProcessingPipeline = chunkProcessingPipeline;
    }

    public bool TryGetLastBounds(out ChunkStreamingBounds bounds)
    {
        bounds = lastBounds;
        return hasLastBounds;
    }

    public bool TryProcessFrame(
        Camera camera,
        ChunkStreamingFrameSettings settings,
        Action<Vector2Int, ChunkStateStore.ChunkState> onChunkLoaded,
        Action<Vector2Int, ChunkStateStore.ChunkState> onChunkUnloading,
        out ChunkStreamingBounds bounds,
        out ChunkProcessingFrameStats processingFrameStats)
    {
        processingFrameStats = default;

        if (chunkStreamingSystem == null || chunkProcessingPipeline == null)
        {
            bounds = default;
            return false;
        }

        if (!ChunkStreamingBoundsCalculator.TryCalculate(
                grid,
                camera,
                worldProfile,
                settings.PreloadChunks,
                settings.UnloadHysteresisChunks,
                out bounds))
        {
            return false;
        }

        lastBounds = bounds;
        hasLastBounds = true;

        chunkStreamingSystem.EnqueueNeededChunks(bounds.LoadMinChunk, bounds.LoadMaxChunk);

        processingFrameStats = chunkProcessingPipeline.ProcessFrame(
            bounds.LoadMinChunk,
            bounds.LoadMaxChunk,
            bounds.UnloadMinChunk,
            bounds.UnloadMaxChunk,
            settings.GenerationBudgetMs,
            settings.MaxChunksPerFrame,
            settings.MaxUnloadRemovalsPerFrame,
            onChunkLoaded,
            onChunkUnloading);

        return true;
    }
}
