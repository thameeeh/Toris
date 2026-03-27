using System.Collections.Generic;
using UnityEngine;

public readonly struct WorldGenDiagnosticsSnapshot
{
    public readonly IReadOnlyCollection<Vector2Int> LoadedChunks;
    public readonly int LoadedChunkCount;
    public readonly int PreloadChunks;
    public readonly int UnloadHysteresisChunks;
    public readonly int ActiveSiteChunkCount;
    public readonly int ActiveSiteCount;
    public readonly int TotalPlacedSiteCount;
    public readonly Camera StreamCamera;
    public readonly WorldProfile Profile;

    public WorldGenDiagnosticsSnapshot(
        IReadOnlyCollection<Vector2Int> loadedChunks,
        int loadedChunkCount,
        int preloadChunks,
        int unloadHysteresisChunks,
        int activeSiteChunkCount,
        int activeSiteCount,
        int totalPlacedSiteCount,
        Camera streamCamera,
        WorldProfile profile)
    {
        LoadedChunks = loadedChunks;
        LoadedChunkCount = loadedChunkCount;
        PreloadChunks = preloadChunks;
        UnloadHysteresisChunks = unloadHysteresisChunks;
        ActiveSiteChunkCount = activeSiteChunkCount;
        ActiveSiteCount = activeSiteCount;
        TotalPlacedSiteCount = totalPlacedSiteCount;
        StreamCamera = streamCamera;
        Profile = profile;
    }
}