public readonly struct WorldGenDiagnosticsSnapshot
{
    public readonly StreamingDiagnosticsSnapshot Streaming;
    public readonly LifecycleDiagnosticsSnapshot Lifecycle;
    public readonly BuildOutputDiagnosticsSnapshot BuildOutputDiagnostics;
    public readonly NavigationDiagnosticsSnapshot Navigation;
    public readonly TransitionDiagnosticsSnapshot Transition;

    public WorldGenDiagnosticsSnapshot(
        StreamingDiagnosticsSnapshot streaming,
        LifecycleDiagnosticsSnapshot lifecycle,
        BuildOutputDiagnosticsSnapshot buildOutputDiagnostics,
        NavigationDiagnosticsSnapshot navigation,
        TransitionDiagnosticsSnapshot transition)
    {
        Streaming = streaming;
        Lifecycle = lifecycle;
        BuildOutputDiagnostics = buildOutputDiagnostics;
        Navigation = navigation;
        Transition = transition;
    }
}
