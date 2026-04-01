public readonly struct ChunkProcessingFrameStats
{
    public readonly int GeneratedChunkCount;
    public readonly double GenerationMsTotal;
    public readonly double ApplyMsTotal;
    public readonly double UnloadMs;

    public ChunkProcessingFrameStats(
        int generatedChunkCount,
        double generationMsTotal,
        double applyMsTotal,
        double unloadMs)
    {
        GeneratedChunkCount = generatedChunkCount;
        GenerationMsTotal = generationMsTotal;
        ApplyMsTotal = applyMsTotal;
        UnloadMs = unloadMs;
    }
}