using UnityEngine;

public readonly struct ChunkStreamingView
{
    public readonly Vector2Int FocusChunk;
    public readonly ChunkStreamingBounds Bounds;

    public ChunkStreamingView(Vector2Int focusChunk, ChunkStreamingBounds bounds)
    {
        FocusChunk = focusChunk;
        Bounds = bounds;
    }
}
