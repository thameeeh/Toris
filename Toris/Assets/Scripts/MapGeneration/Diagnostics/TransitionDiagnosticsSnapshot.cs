public readonly struct TransitionDiagnosticsSnapshot
{
    public readonly int CurrentBiomeIndex;
    public readonly float GateCooldownRemainingSeconds;
    public readonly bool SceneTransitionLoading;

    public TransitionDiagnosticsSnapshot(
        int currentBiomeIndex,
        float gateCooldownRemainingSeconds,
        bool sceneTransitionLoading)
    {
        CurrentBiomeIndex = currentBiomeIndex;
        GateCooldownRemainingSeconds = gateCooldownRemainingSeconds;
        SceneTransitionLoading = sceneTransitionLoading;
    }
}
