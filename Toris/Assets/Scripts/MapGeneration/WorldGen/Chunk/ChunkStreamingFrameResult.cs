public readonly struct ChunkStreamingFrameResult
{
    public readonly bool ProcessedFrame;
    public readonly bool HasView;
    public readonly ChunkStreamingView View;
    public readonly ChunkProcessingFrameStats ProcessingStats;

    public ChunkStreamingFrameResult(
        bool processedFrame,
        ChunkStreamingView view,
        ChunkProcessingFrameStats processingStats)
    {
        ProcessedFrame = processedFrame;
        HasView = processedFrame;
        View = view;
        ProcessingStats = processingStats;
    }
}
