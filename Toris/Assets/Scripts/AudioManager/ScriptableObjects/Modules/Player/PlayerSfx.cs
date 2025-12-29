using UnityEngine;

public sealed class PlayerSfx : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private PlayerBowController bow;
    [SerializeField] private PlayerController playerController; // for dash
    [SerializeField] private PlayerMotor motor;
    [SerializeField] private Rigidbody2D rb;

    [Header("Modules (ScriptableObjects)")]
    [SerializeField] private PlayerSfxModule[] modules;

    // Centralized runtime state (IMPORTANT: keep this out of ScriptableObjects)
    private AudioVoiceHandle footstepLoopHandle;

    private DashAbility dash;
    private PlayerSfxContext ctx;

    public AudioVoiceHandle FootstepLoopHandle => footstepLoopHandle;
    public bool IsFootstepLoopActive => footstepLoopHandle.IsValid;

    private void Awake()
    {
        // Resolve dependencies if not wired
        if (!bow) bow = GetComponent<PlayerBowController>();
        if (!playerController) playerController = GetComponent<PlayerController>();
        if (!motor) motor = GetComponent<PlayerMotor>();
        if (!rb) rb = GetComponent<Rigidbody2D>();

        dash = playerController != null ? playerController.DashAbility : null;

        ctx = new PlayerSfxContext(
            hub: this,
            transform: transform,
            bow: bow,
            dash: dash,
            motor: motor,
            rb: rb);

        footstepLoopHandle = AudioVoiceHandle.Invalid;
    }

    private void OnEnable()
    {
        // Hook gameplay events (no coupling into gameplay scripts; just listening)
        if (bow != null)
        {
            bow.DrawStarted += OnBowDrawStarted;
            bow.ShotFired += OnBowShotFired;
            bow.DryReleased += OnBowDryReleased;
        }

        if (dash != null)
        {
            dash.Activated += OnDashStarted;
            dash.Completed += OnDashCompleted;
        }

        // Initialize modules
        if (modules != null)
        {
            for (int i = 0; i < modules.Length; i++)
            {
                if (modules[i] == null) continue;
                modules[i].Initialize(ctx);
            }
        }
    }

    private void OnDisable()
    {
        if (bow != null)
        {
            bow.DrawStarted -= OnBowDrawStarted;
            bow.ShotFired -= OnBowShotFired;
            bow.DryReleased -= OnBowDryReleased;
        }

        if (dash != null)
        {
            dash.Activated -= OnDashStarted;
            dash.Completed -= OnDashCompleted;
        }

        StopFootstepLoop(0.05f);
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;

        if (modules == null) return;

        for (int i = 0; i < modules.Length; i++)
        {
            var m = modules[i];
            if (m == null) continue;
            m.Tick(ctx, dt);
        }
    }

    // ---------- Hub-owned loop control ----------
    public void StartFootstepLoop(string sfxId, SfxPlayRequest request)
    {
        if (!ctx.HasAudio) return;
        if (footstepLoopHandle.IsValid) return;

        footstepLoopHandle = AudioBootstrap.Sfx.PlayAttachedLoop(
            sfxId,
            transform,
            Vector3.zero,
            request
        );
    }

    public void StopFootstepLoop(float fadeOutSeconds)
    {
        if (!ctx.HasAudio) { footstepLoopHandle = AudioVoiceHandle.Invalid; return; }
        if (!footstepLoopHandle.IsValid) return;

        AudioBootstrap.Sfx.Stop(footstepLoopHandle, fadeOutSeconds);
        footstepLoopHandle = AudioVoiceHandle.Invalid;
    }

    // ---------- Event forwarders ----------
    private void OnBowDrawStarted()
    {
        if (modules == null) return;
        for (int i = 0; i < modules.Length; i++) modules[i]?.OnBowDrawStarted(ctx);
    }

    private void OnBowShotFired()
    {
        if (modules == null) return;
        for (int i = 0; i < modules.Length; i++) modules[i]?.OnBowShotFired(ctx);
    }

    private void OnBowDryReleased()
    {
        if (modules == null) return;
        for (int i = 0; i < modules.Length; i++) modules[i]?.OnBowDryReleased(ctx);
    }

    private void OnDashStarted()
    {
        if (modules == null) return;
        for (int i = 0; i < modules.Length; i++) modules[i]?.OnDashStarted(ctx);
    }

    private void OnDashCompleted()
    {
        if (modules == null) return;
        for (int i = 0; i < modules.Length; i++) modules[i]?.OnDashCompleted(ctx);
    }
}
