using System.Collections.Generic;
using UnityEngine;

public sealed class ChunkProcessingPipeline
{
    private readonly WorldProfile worldProfile;
    private readonly WorldNavigationLifecycle worldNavigationLifecycle;
    private readonly ChunkGenerator chunkGenerator;
    private readonly TilemapApplier tilemapApplier;
    private readonly WorldFeatureLifecycleSystem worldFeatureLifecycleSystem;
    private readonly ChunkStreamingSystem chunkStreamingSystem;

    public ChunkProcessingPipeline(
        WorldProfile worldProfile,
        WorldNavigationLifecycle worldNavigationLifecycle,
        ChunkGenerator chunkGenerator,
        TilemapApplier tilemapApplier,
        WorldFeatureLifecycleSystem worldFeatureLifecycleSystem,
        ChunkStreamingSystem chunkStreamingSystem)
    {
        this.worldProfile = worldProfile;
        this.worldNavigationLifecycle = worldNavigationLifecycle;
        this.chunkGenerator = chunkGenerator;
        this.tilemapApplier = tilemapApplier;
        this.worldFeatureLifecycleSystem = worldFeatureLifecycleSystem;
        this.chunkStreamingSystem = chunkStreamingSystem;
    }

    public ChunkProcessingFrameStats ProcessFrame(
        Vector2Int loadMinChunk,
        Vector2Int loadMaxChunk,
        Vector2Int unloadMinChunk,
        Vector2Int unloadMaxChunk,
        double generationBudgetMs,
        int maxChunksPerFrame,
        int maxUnloadRemovalsPerFrame)
    {
        if (worldProfile == null || chunkGenerator == null || tilemapApplier == null || chunkStreamingSystem == null)
            return default;

        long frameStartTicks = System.Diagnostics.Stopwatch.GetTimestamp();
        long stopwatchFrequency = System.Diagnostics.Stopwatch.Frequency;

        double ElapsedMs()
            => (System.Diagnostics.Stopwatch.GetTimestamp() - frameStartTicks) * 1000.0 / stopwatchFrequency;

        int hardChunkCap = Mathf.Max(0, maxChunksPerFrame);
        int generatedChunkCount = 0;

        long totalGenerationTicks = 0;
        long totalApplyTicks = 0;

        double estimatedChunkMs = 6.0;

        while (generatedChunkCount < hardChunkCap)
        {
            double elapsedMs = ElapsedMs();
            double remainingMs = generationBudgetMs - elapsedMs;
            if (remainingMs <= 0.0)
                break;

            if (generatedChunkCount > 0)
            {
                double generationMsSoFar = totalGenerationTicks * 1000.0 / stopwatchFrequency;
                double applyMsSoFar = totalApplyTicks * 1000.0 / stopwatchFrequency;
                estimatedChunkMs = (generationMsSoFar + applyMsSoFar) / generatedChunkCount;
            }

            const double safetyMs = 0.25;
            if (generatedChunkCount > 0 && remainingMs < (estimatedChunkMs + safetyMs))
                break;

            if (!chunkStreamingSystem.TryDequeueNextChunk(loadMinChunk, loadMaxChunk, out Vector2Int chunkCoord))
                break;

            long generationStartTicks = System.Diagnostics.Stopwatch.GetTimestamp();
            ChunkResult chunkResult = chunkGenerator.GenerateChunk(chunkCoord);
            long generationEndTicks = System.Diagnostics.Stopwatch.GetTimestamp();

            tilemapApplier.Apply(chunkResult);

            worldNavigationLifecycle?.BuildChunk(chunkCoord, worldProfile.chunkSize);
            worldFeatureLifecycleSystem?.ActivateChunk(chunkCoord);

            long applyEndTicks = System.Diagnostics.Stopwatch.GetTimestamp();

            chunkStreamingSystem.MarkChunkLoaded(chunkCoord);

            totalGenerationTicks += (generationEndTicks - generationStartTicks);
            totalApplyTicks += (applyEndTicks - generationEndTicks);
            generatedChunkCount++;
        }

        long unloadStartTicks = System.Diagnostics.Stopwatch.GetTimestamp();

        UnloadChunksOutside(
            unloadMinChunk,
            unloadMaxChunk,
            maxUnloadRemovalsPerFrame);

        long unloadEndTicks = System.Diagnostics.Stopwatch.GetTimestamp();

        double generationMsTotal = totalGenerationTicks * 1000.0 / stopwatchFrequency;
        double applyMsTotal = totalApplyTicks * 1000.0 / stopwatchFrequency;
        double unloadMs = (unloadEndTicks - unloadStartTicks) * 1000.0 / stopwatchFrequency;

        return new ChunkProcessingFrameStats(
            generatedChunkCount,
            generationMsTotal,
            applyMsTotal,
            unloadMs);
    }

    public void ClearLoadedChunks()
    {
        if (chunkStreamingSystem == null || tilemapApplier == null || worldProfile == null)
            return;

        IReadOnlyCollection<Vector2Int> loadedChunks = chunkStreamingSystem.LoadedChunks;
        if (loadedChunks == null || loadedChunks.Count == 0)
            return;

        List<Vector2Int> loadedChunkCopy = new List<Vector2Int>(loadedChunks);
        foreach (Vector2Int chunkCoord in loadedChunkCopy)
        {
            worldFeatureLifecycleSystem?.DeactivateChunk(chunkCoord);
            tilemapApplier.ClearChunk(chunkCoord, worldProfile.chunkSize);
            worldNavigationLifecycle?.ClearChunk(chunkCoord);
            chunkStreamingSystem.MarkChunkUnloaded(chunkCoord);
        }
    }

    public void HardResetWorld()
    {
        ClearLoadedChunks();
        tilemapApplier?.ClearAll();
    }

    private void UnloadChunksOutside(
        Vector2Int keepMinChunk,
        Vector2Int keepMaxChunk,
        int maxUnloadRemovalsPerFrame)
    {
        if (chunkStreamingSystem == null)
            return;

        List<Vector2Int> chunksToUnload = chunkStreamingSystem.CollectChunksToUnload(
            keepMinChunk,
            keepMaxChunk,
            maxUnloadRemovalsPerFrame);

        if (chunksToUnload == null || chunksToUnload.Count == 0)
            return;

        for (int i = 0; i < chunksToUnload.Count; i++)
        {
            Vector2Int chunkCoord = chunksToUnload[i];

            worldFeatureLifecycleSystem?.DeactivateChunk(chunkCoord);
            tilemapApplier.ClearChunk(chunkCoord, worldProfile.chunkSize);
            worldNavigationLifecycle?.ClearChunk(chunkCoord);

            chunkStreamingSystem.MarkChunkUnloaded(chunkCoord);
        }
    }
}
