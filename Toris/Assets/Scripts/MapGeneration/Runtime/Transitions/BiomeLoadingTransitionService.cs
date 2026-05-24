using UnityEngine;

public sealed class BiomeLoadingTransitionService : IGateTransitionService
{
    private const string TransitionName = "Biome Gate";
    private const string LoadingMessage = "Switching Biomes";

    private readonly WorldTransitionSystem worldTransitionSystem;
    private readonly SceneTransitionService sceneTransitionService;
    private readonly float streamingReadyTimeoutSeconds;
    private readonly float postReadyHoldSeconds;
    private WorldStreamingRuntime worldStreamingRuntime;

    public BiomeLoadingTransitionService(
        WorldTransitionSystem worldTransitionSystem,
        SceneTransitionService sceneTransitionService,
        float streamingReadyTimeoutSeconds,
        float postReadyHoldSeconds)
    {
        this.worldTransitionSystem = worldTransitionSystem;
        this.sceneTransitionService = sceneTransitionService;
        this.streamingReadyTimeoutSeconds = Mathf.Max(0.1f, streamingReadyTimeoutSeconds);
        this.postReadyHoldSeconds = Mathf.Max(0f, postReadyHoldSeconds);
    }

    public void AttachStreamingRuntime(WorldStreamingRuntime worldStreamingRuntime)
    {
        this.worldStreamingRuntime = worldStreamingRuntime;
    }

    public bool UseGate(Vector2Int gateTile)
    {
        if (worldTransitionSystem == null || !worldTransitionSystem.CanUseGate())
            return false;

        if (sceneTransitionService == null)
            return worldTransitionSystem.UseGate(gateTile);

        return sceneTransitionService.TryRunLoadingTransition(
            TransitionName,
            () =>
            {
                // World-generation handoff: the loading overlay is now covering the old biome.
                worldTransitionSystem.UseGate(gateTile);
            },
            IsWorldStreamingReadyForReveal,
            streamingReadyTimeoutSeconds,
            postReadyHoldSeconds,
            LoadingMessage,
            playTeleportArriveOnComplete: true);
    }

    private bool IsWorldStreamingReadyForReveal()
    {
        // Streaming handoff: wait until the new biome has at least one settled visible chunk set.
        return worldStreamingRuntime == null || worldStreamingRuntime.IsCurrentViewSettled();
    }
}
