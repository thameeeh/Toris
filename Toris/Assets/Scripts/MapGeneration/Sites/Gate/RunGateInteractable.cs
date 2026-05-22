using UnityEngine;
using UnityEngine.Serialization;

public class RunGateInteractable : MonoBehaviour, IInteractable, IWorldSiteBridge
{
    [Header("Scene Connection")]
    [SerializeField] private string sceneA;
    [SerializeField] private string sceneB;
    [FormerlySerializedAs("sceneTransitionServiceOverride")]
    [SerializeField] private MonoBehaviour runGateTransitionServiceOverride;
    [SerializeField] private RunStartCheckpointService runStartCheckpointService;

    [Header("SFX")]
    [SerializeField] private string teleportLeaveSfxId = "world_teleport_leave";
    [SerializeField] private string teleportLoopSfxId = "world_teleport_loop";
    [SerializeField] private Vector3 sfxLocalOffset = Vector3.zero;
    [SerializeField, Range(0f, 2f)] private float sfxVolumeMultiplier = 1f;
    [SerializeField, Min(0f)] private float loopFadeInSeconds = 0.08f;
    [SerializeField, Min(0f)] private float loopFadeOutSeconds = 0.05f;

    private IRunGateTransitionService runGateTransitionService;
    private AudioVoiceHandle teleportLoopHandle;

    private void OnEnable()
    {
        TryStartTeleportLoop();
    }

    private void Start()
    {
        TryStartTeleportLoop();
    }

    private void OnDisable()
    {
        StopTeleportLoop();
    }

    public void Interact(GameObject interactor)
    {
        runGateTransitionService ??= ResolveRunGateTransitionService();
        if (runGateTransitionService == null)
        {
            Debug.LogWarning("RunGateInteractable: run gate transition service unavailable.", this);
            return;
        }

        // Death respawn uses the MainArea -> ProceduralTiles checkpoint as its reset source.
        runStartCheckpointService ??= FindFirstObjectByType<RunStartCheckpointService>();
        runStartCheckpointService?.CaptureCheckpointIfRunStart(sceneA, sceneB);

        StopTeleportLoop();
        PlayTeleportLeaveSfx();
        runGateTransitionService.UseRunGate(sceneA, sceneB);
    }

    public void Initialize(WorldSiteContext siteContext)
    {
        runGateTransitionService = siteContext.RunGateTransitionService ?? ResolveRunGateTransitionService();
    }

    private IRunGateTransitionService ResolveRunGateTransitionService()
    {
        if (runGateTransitionServiceOverride is IRunGateTransitionService overrideService)
            return overrideService;

        if (SceneTransitionService.Instance != null)
            return SceneTransitionService.Instance;

        SceneTransitionService localSceneTransitionService = FindFirstObjectByType<SceneTransitionService>();
        if (localSceneTransitionService != null)
            return localSceneTransitionService;

        return null;
    }

    private void TryStartTeleportLoop()
    {
        if (!Application.isPlaying || teleportLoopHandle.IsValid || AudioBootstrap.Sfx == null || string.IsNullOrWhiteSpace(teleportLoopSfxId))
            return;

        // SFX-only hook: the gate idle loop follows this gate while it is active and does not affect interaction state.
        teleportLoopHandle = AudioBootstrap.Sfx.PlayAttachedLoop(
            teleportLoopSfxId,
            transform,
            sfxLocalOffset,
            MakeSfxRequest(force2D: false, loopFadeInSeconds));
    }

    private void StopTeleportLoop()
    {
        if (!teleportLoopHandle.IsValid || AudioBootstrap.Sfx == null)
            return;

        AudioBootstrap.Sfx.Stop(teleportLoopHandle, loopFadeOutSeconds);
        teleportLoopHandle = AudioVoiceHandle.Invalid;
    }

    private void PlayTeleportLeaveSfx()
    {
        if (AudioBootstrap.Sfx == null || string.IsNullOrWhiteSpace(teleportLeaveSfxId))
            return;

        // SFX-only hook: the leave one-shot plays after the gate accepts interaction and before scene loading starts.
        AudioBootstrap.Sfx.PlayAt(
            teleportLeaveSfxId,
            transform.TransformPoint(sfxLocalOffset),
            MakeSfxRequest(force2D: false));
    }

    private SfxPlayRequest MakeSfxRequest(bool force2D, float fadeInSeconds = 0f)
    {
        SfxPlayRequest request = SfxPlayRequest.Default;
        request.volumeMultiplier = sfxVolumeMultiplier;
        request.fadeInSeconds = fadeInSeconds;
        request.force2D = force2D;
        return request;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (runGateTransitionServiceOverride != null && runGateTransitionServiceOverride is not IRunGateTransitionService)
        {
            runGateTransitionServiceOverride = null;
        }
    }
#endif
}
