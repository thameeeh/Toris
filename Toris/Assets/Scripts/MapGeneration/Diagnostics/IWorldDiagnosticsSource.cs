public interface IWorldDiagnosticsSource
{
    WorldContext Context { get; }

    WorldGenDiagnosticsSnapshot CreateDiagnosticsSnapshot();
}
