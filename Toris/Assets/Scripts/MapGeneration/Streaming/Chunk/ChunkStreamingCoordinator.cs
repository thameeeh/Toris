using UnityEngine;

public sealed class ChunkStreamingCoordinator
{
    private readonly WorldProfile worldProfile;
    private readonly Grid grid;
    private readonly ChunkStreamingSystem chunkStreamingSystem;
    private readonly ChunkProcessingPipeline chunkProcessingPipeline;

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

    public ChunkStreamingFrameResult ProcessFrame(
        ChunkStreamingRequest request)
    {
        if (chunkStreamingSystem == null || chunkProcessingPipeline == null)
            return default;

        if (!ChunkStreamingBoundsCalculator.TryCalculateView(
                grid,
                request.Camera,
                worldProfile,
                request.Settings.PreloadChunks,
                request.Settings.UnloadHysteresisChunks,
                out ChunkStreamingView view))
        {
            return default;
        }

        chunkStreamingSystem.SetStreamingAnchor(view.FocusChunk);
        chunkStreamingSystem.EnqueueNeededChunks(view.Bounds.LoadMinChunk, view.Bounds.LoadMaxChunk);

        ChunkProcessingFrameStats processingFrameStats = chunkProcessingPipeline.ProcessFrame(
            view.Bounds.LoadMinChunk,
            view.Bounds.LoadMaxChunk,
            view.Bounds.UnloadMinChunk,
            view.Bounds.UnloadMaxChunk,
            request.Settings.GenerationBudgetMs,
            request.Settings.MaxChunksPerFrame,
            request.Settings.MaxUnloadRemovalsPerFrame);

        ChunkStreamingFrameResult frameResult = new ChunkStreamingFrameResult(
            true,
            view,
            processingFrameStats);

        return frameResult;
    }
}
