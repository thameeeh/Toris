public sealed class WorldRuntimeState
{
    public ChunkStateStore ChunkStates { get; } = new ChunkStateStore();

    public void Clear()
    {
        ChunkStates.Clear();
    }
}