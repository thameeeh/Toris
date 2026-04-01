using UnityEngine;

public readonly struct ChunkStreamingBounds
{
    public readonly Vector2Int LoadMinChunk;
    public readonly Vector2Int LoadMaxChunk;
    public readonly Vector2Int UnloadMinChunk;
    public readonly Vector2Int UnloadMaxChunk;

    public ChunkStreamingBounds(
        Vector2Int loadMinChunk,
        Vector2Int loadMaxChunk,
        Vector2Int unloadMinChunk,
        Vector2Int unloadMaxChunk)
    {
        LoadMinChunk = loadMinChunk;
        LoadMaxChunk = loadMaxChunk;
        UnloadMinChunk = unloadMinChunk;
        UnloadMaxChunk = unloadMaxChunk;
    }
}
