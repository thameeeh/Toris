using UnityEngine;

public readonly struct ChunkStreamingRequest
{
    public readonly Camera Camera;
    public readonly ChunkStreamingFrameSettings Settings;

    public ChunkStreamingRequest(Camera camera, ChunkStreamingFrameSettings settings)
    {
        Camera = camera;
        Settings = settings;
    }
}
