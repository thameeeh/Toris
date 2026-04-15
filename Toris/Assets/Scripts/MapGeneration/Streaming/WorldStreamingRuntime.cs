using UnityEngine;

public sealed class WorldStreamingRuntime
{
    private const double WarnUnloadMs = 2.0;
    private const double WarnGenerationMs = 10.0;
    private const double WarnApplyMs = 2.0;
    private const int MaxUnloadRemovalsPerFrame = 1;

    private readonly WorldProfile worldProfile;
    private readonly ChunkStreamingSystem chunkStreamingSystem;
    private readonly ChunkStreamingCoordinator chunkStreamingCoordinator;
    private readonly ChunkProcessingPipeline chunkProcessingPipeline;
    private readonly Camera streamCamera;
    private readonly int preloadChunks;
    private readonly int unloadHysteresisChunks;
    private readonly int maxChunksPerFrame;
    private readonly float generationBudgetMs;

    private ChunkStreamingFrameResult lastProcessedFrameResult;
    private bool hasLastProcessedFrameResult;

    public WorldStreamingRuntime(
        WorldProfile worldProfile,
        ChunkStreamingSystem chunkStreamingSystem,
        ChunkStreamingCoordinator chunkStreamingCoordinator,
        ChunkProcessingPipeline chunkProcessingPipeline,
        Camera streamCamera,
        int preloadChunks,
        int unloadHysteresisChunks,
        int maxChunksPerFrame,
        float generationBudgetMs)
    {
        this.worldProfile = worldProfile;
        this.chunkStreamingSystem = chunkStreamingSystem;
        this.chunkStreamingCoordinator = chunkStreamingCoordinator;
        this.chunkProcessingPipeline = chunkProcessingPipeline;
        this.streamCamera = streamCamera;
        this.preloadChunks = Mathf.Max(0, preloadChunks);
        this.unloadHysteresisChunks = Mathf.Max(0, unloadHysteresisChunks);
        this.maxChunksPerFrame = Mathf.Max(0, maxChunksPerFrame);
        this.generationBudgetMs = Mathf.Max(0.1f, generationBudgetMs);
    }

    public void ProcessFrame()
    {
        Camera runtimeCamera = ResolveStreamingCamera();
        if (runtimeCamera == null || chunkStreamingCoordinator == null)
            return;

        ChunkStreamingFrameSettings streamingSettings = new ChunkStreamingFrameSettings(
            preloadChunks,
            unloadHysteresisChunks,
            generationBudgetMs,
            maxChunksPerFrame,
            MaxUnloadRemovalsPerFrame);

        ChunkStreamingFrameResult streamingFrameResult = chunkStreamingCoordinator.ProcessFrame(
            new ChunkStreamingRequest(runtimeCamera, streamingSettings));

        if (!streamingFrameResult.ProcessedFrame)
            return;

        lastProcessedFrameResult = streamingFrameResult;
        hasLastProcessedFrameResult = true;

        LogIfFrameExceedsWarningThresholds(streamingFrameResult.ProcessingStats);
    }

    public void Reset()
    {
        chunkProcessingPipeline?.HardResetWorld();
        chunkStreamingSystem?.Reset();
        lastProcessedFrameResult = default;
        hasLastProcessedFrameResult = false;
    }

    public StreamingDiagnosticsSnapshot CreateDiagnosticsSnapshot()
    {
        bool hasStreamingBounds = hasLastProcessedFrameResult && lastProcessedFrameResult.ProcessedFrame;
        ChunkStreamingFrameResult streamingFrameResult = hasStreamingBounds
            ? lastProcessedFrameResult
            : default;

        bool streamingAnchorInitialized = chunkStreamingSystem != null && chunkStreamingSystem.AnchorInitialized;
        Vector2Int streamingAnchorChunk = chunkStreamingSystem != null
            ? chunkStreamingSystem.StreamingAnchorChunk
            : default;

        if (!streamingAnchorInitialized && hasStreamingBounds)
        {
            streamingAnchorInitialized = true;
            streamingAnchorChunk = streamingFrameResult.View.FocusChunk;
        }

        return new StreamingDiagnosticsSnapshot(
            chunkStreamingSystem != null ? chunkStreamingSystem.LoadedChunks : null,
            chunkStreamingSystem != null ? chunkStreamingSystem.LoadedChunkCount : 0,
            chunkStreamingSystem != null ? chunkStreamingSystem.GenerationQueueCount : 0,
            chunkStreamingSystem != null ? chunkStreamingSystem.QueuedChunkCount : 0,
            preloadChunks,
            unloadHysteresisChunks,
            hasStreamingBounds,
            hasStreamingBounds ? streamingFrameResult.View.Bounds : default,
            streamingAnchorInitialized,
            streamingAnchorChunk,
            worldProfile != null ? worldProfile.chunkSize : 0);
    }

    private Camera ResolveStreamingCamera()
    {
        return streamCamera != null ? streamCamera : Camera.main;
    }

    private void LogIfFrameExceedsWarningThresholds(ChunkProcessingFrameStats processingFrameStats)
    {
        if (processingFrameStats.UnloadMs < WarnUnloadMs &&
            processingFrameStats.GenerationMsTotal < WarnGenerationMs &&
            processingFrameStats.ApplyMsTotal < WarnApplyMs)
        {
            return;
        }

        Debug.Log(
            $"[WorldGen] unload={(int)processingFrameStats.UnloadMs}ms, " +
            $"genChunks={processingFrameStats.GeneratedChunkCount}/{maxChunksPerFrame} " +
            $"budget={generationBudgetMs:F1}ms " +
            $"gen={processingFrameStats.GenerationMsTotal:F2}ms " +
            $"apply={processingFrameStats.ApplyMsTotal:F2}ms, " +
            $"queue={(chunkStreamingSystem != null ? chunkStreamingSystem.GenerationQueueCount : 0)} " +
            $"loaded={(chunkStreamingSystem != null ? chunkStreamingSystem.LoadedChunkCount : 0)} " +
            $"chunkSize={worldProfile.chunkSize}"
        );
    }
}
