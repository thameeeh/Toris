public readonly struct ChunkStreamingFrameResult
{
    public readonly bool ProcessedFrame;
    public readonly ChunkStreamingView View;
    public readonly ChunkProcessingFrameStats ProcessingStats;

    public ChunkStreamingFrameResult(
        bool processedFrame,
        ChunkStreamingView view,
        ChunkProcessingFrameStats processingStats)
    {
        ProcessedFrame = processedFrame;
        View = view;
        ProcessingStats = processingStats;
    }
}
