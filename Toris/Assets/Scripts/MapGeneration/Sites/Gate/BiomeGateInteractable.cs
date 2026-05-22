using UnityEngine;

public class BiomeGateInteractable : MonoBehaviour, IInteractable, IPoolable, IWorldSiteBridge
{
    [Header("SFX")]
    [SerializeField] private string teleportLeaveSfxId = "world_teleport_leave";
    [SerializeField] private string teleportLoopSfxId = "world_teleport_loop";
    [SerializeField] private Vector3 sfxLocalOffset = Vector3.zero;
    [SerializeField, Range(0f, 2f)] private float sfxVolumeMultiplier = 1f;
    [SerializeField, Min(0f)] private float loopFadeInSeconds = 0.08f;
    [SerializeField, Min(0f)] private float loopFadeOutSeconds = 0.05f;

    private IGateTransitionService gateTransitionService;
    private Vector2Int gateTile;
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

    public void Initialize(IGateTransitionService gateTransitionService, Vector2Int gateTile)
    {
        this.gateTransitionService = gateTransitionService;
        this.gateTile = gateTile;

        TryStartTeleportLoop();

        // disable colliders/visuals, reset them here
    }

    public void Interact(GameObject interactor)
    {
        if (gateTransitionService == null)
        {
            Debug.LogWarning("GateInteractable: gate transition service not injected.", this);
            return;
        }

        if (gateTransitionService.UseGate(gateTile))
        {
            StopTeleportLoop();
            PlayTeleportLeaveSfx();
        }
    }

    public void OnSpawned()
    {
        TryStartTeleportLoop();

        // reset animator/highlight/prompt when you add them
    }

    public void OnDespawned()
    {
        StopAllCoroutines();
        StopTeleportLoop();

        gateTransitionService = null;
        gateTile = default;
    }

    public void Initialize(WorldSiteContext siteContext)
    {
        Initialize(siteContext.GateTransitionService, siteContext.Placement.CenterTile);
    }

    private void TryStartTeleportLoop()
    {
        if (!Application.isPlaying || teleportLoopHandle.IsValid || AudioBootstrap.Sfx == null || string.IsNullOrWhiteSpace(teleportLoopSfxId))
            return;

        // SFX-only hook: the biome gate idle loop follows this gate while it is active and does not affect interaction state.
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

        // SFX-only hook: the leave one-shot plays after the biome gate transition request is accepted.
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
}
