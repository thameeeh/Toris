using System.Collections.Generic;
using UnityEngine;

public readonly struct WorldGenDiagnosticsSnapshot
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
    public readonly int ActiveSiteChunkCount;
    public readonly int ActivePersistentSiteCount;
    public readonly int ActiveSiteCount;
    public readonly int TotalPlacedSiteCount;
    public readonly int LoadedNavChunkCount;
    public readonly bool NavigationContributionsBound;
    public readonly int CurrentBiomeIndex;
    public readonly float GateCooldownRemainingSeconds;
    public readonly bool SceneTransitionLoading;
    public readonly WorldProfile Profile;

    public WorldGenDiagnosticsSnapshot(
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
        int activeSiteChunkCount,
        int activePersistentSiteCount,
        int activeSiteCount,
        int totalPlacedSiteCount,
        int loadedNavChunkCount,
        bool navigationContributionsBound,
        int currentBiomeIndex,
        float gateCooldownRemainingSeconds,
        bool sceneTransitionLoading,
        WorldProfile profile)
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
        ActiveSiteChunkCount = activeSiteChunkCount;
        ActivePersistentSiteCount = activePersistentSiteCount;
        ActiveSiteCount = activeSiteCount;
        TotalPlacedSiteCount = totalPlacedSiteCount;
        LoadedNavChunkCount = loadedNavChunkCount;
        NavigationContributionsBound = navigationContributionsBound;
        CurrentBiomeIndex = currentBiomeIndex;
        GateCooldownRemainingSeconds = gateCooldownRemainingSeconds;
        SceneTransitionLoading = sceneTransitionLoading;
        Profile = profile;
    }
}
