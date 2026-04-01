using System.Collections.Generic;
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

    public IReadOnlyCollection<Vector2Int> LoadedChunks => chunkStreamingSystem != null ? chunkStreamingSystem.LoadedChunks : null;
    public int LoadedChunkCount => chunkStreamingSystem != null ? chunkStreamingSystem.LoadedChunkCount : 0;
    public int GenerationQueueCount => chunkStreamingSystem != null ? chunkStreamingSystem.GenerationQueueCount : 0;
    public int QueuedChunkCount => chunkStreamingSystem != null ? chunkStreamingSystem.QueuedChunkCount : 0;
    public Vector2Int StreamingAnchorChunk => chunkStreamingSystem != null ? chunkStreamingSystem.StreamingAnchorChunk : default;
    public bool StreamingAnchorInitialized => chunkStreamingSystem != null && chunkStreamingSystem.AnchorInitialized;
    public int PreloadChunks => preloadChunks;
    public int UnloadHysteresisChunks => unloadHysteresisChunks;
    public bool HasLastProcessedFrameResult => hasLastProcessedFrameResult && lastProcessedFrameResult.ProcessedFrame;

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
        chunkProcessingPipeline?.ClearLoadedChunks();
        chunkStreamingSystem?.Reset();
        lastProcessedFrameResult = default;
        hasLastProcessedFrameResult = false;
    }

    public bool TryGetLastProcessedFrameResult(out ChunkStreamingFrameResult frameResult)
    {
        if (HasLastProcessedFrameResult)
        {
            frameResult = lastProcessedFrameResult;
            return true;
        }

        frameResult = default;
        return false;
    }

    public StreamingDiagnosticsSnapshot CreateDiagnosticsSnapshot()
    {
        ChunkStreamingFrameResult streamingFrameResult = default;
        bool hasStreamingBounds = TryGetLastProcessedFrameResult(out streamingFrameResult);

        bool streamingAnchorInitialized = StreamingAnchorInitialized;
        Vector2Int streamingAnchorChunk = StreamingAnchorChunk;

        if (!streamingAnchorInitialized && hasStreamingBounds)
        {
            streamingAnchorInitialized = true;
            streamingAnchorChunk = streamingFrameResult.View.FocusChunk;
        }

        return new StreamingDiagnosticsSnapshot(
            LoadedChunks,
            LoadedChunkCount,
            GenerationQueueCount,
            QueuedChunkCount,
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
            $"queue={GenerationQueueCount} " +
            $"loaded={LoadedChunkCount} " +
            $"chunkSize={worldProfile.chunkSize}"
        );
    }
}
