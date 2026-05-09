using System.Collections.Generic;
using UnityEngine;

public sealed class PlayerSfx : MonoBehaviour
{
    [Header("Rules (ScriptableObjects)")]
    [SerializeField] private PlayerSfxRuleSO[] rules;

    [Header("Legacy Modules")]
    [SerializeField] private PlayerSfxModule[] legacyModules;

    private readonly Dictionary<string, AudioVoiceHandle> loopHandles = new();
    private readonly Dictionary<PlayerSfxRuleSO, float> ruleCooldownTimes = new();
    private AudioVoiceHandle footstepLoopHandle;

    private PlayerSfxEventContext latestContext;
    private bool hasLatestContext;

    public AudioVoiceHandle FootstepLoopHandle => footstepLoopHandle;
    public bool IsFootstepLoopActive => footstepLoopHandle.IsValid;

    private void Awake()
    {
        footstepLoopHandle = AudioVoiceHandle.Invalid;
    }

    private void OnDisable()
    {
        StopFootstepLoop(0.05f);
        StopAllLoops(0.05f);
        ruleCooldownTimes.Clear();
        hasLatestContext = false;
    }

    private void Update()
    {
        if (legacyModules == null || !hasLatestContext)
            return;

        PlayerSfxContext legacyContext = CreateLegacyContext(latestContext);
        float unscaledDeltaTime = Time.unscaledDeltaTime;
        for (int i = 0; i < legacyModules.Length; i++)
        {
            legacyModules[i]?.Tick(legacyContext, unscaledDeltaTime);
        }
    }

    public void InitializeRuntime(in PlayerSfxEventContext context)
    {
        latestContext = context;
        hasLatestContext = true;

        if (legacyModules == null)
            return;

        PlayerSfxContext legacyContext = CreateLegacyContext(context);
        for (int i = 0; i < legacyModules.Length; i++)
        {
            legacyModules[i]?.Initialize(legacyContext);
        }
    }

    public void DisposeRuntime(in PlayerSfxEventContext context)
    {
        if (legacyModules != null)
        {
            PlayerSfxContext legacyContext = CreateLegacyContext(context);
            for (int i = 0; i < legacyModules.Length; i++)
            {
                legacyModules[i]?.Dispose(legacyContext);
            }
        }

        StopFootstepLoop(0.05f);
        StopAllLoops(0.05f);
        ruleCooldownTimes.Clear();
        hasLatestContext = false;
    }

    public void HandleEvent(in PlayerSfxEventContext context)
    {
        latestContext = context;
        hasLatestContext = true;

        if (rules != null)
        {
            for (int i = 0; i < rules.Length; i++)
            {
                rules[i]?.Evaluate(context);
            }
        }

        DispatchLegacyModules(context);
    }

    public bool TryUseRuleCooldown(PlayerSfxRuleSO rule, float cooldownSeconds)
    {
        if (rule == null)
            return false;

        if (cooldownSeconds <= 0f)
            return true;

        float now = Time.time;
        if (ruleCooldownTimes.TryGetValue(rule, out float lastPlayedTime) &&
            now - lastPlayedTime < cooldownSeconds)
        {
            return false;
        }

        ruleCooldownTimes[rule] = now;
        return true;
    }

    public AudioVoiceHandle PlayOneShot(string sfxId, Vector3 worldPosition, SfxPlayRequest request)
    {
        if (!HasAudio || string.IsNullOrWhiteSpace(sfxId))
            return AudioVoiceHandle.Invalid;

        return AudioBootstrap.Sfx.PlayAt(sfxId, worldPosition, request);
    }

    public AudioVoiceHandle PlayAttachedOneShot(string sfxId, Vector3 localOffset, SfxPlayRequest request)
    {
        if (!HasAudio || string.IsNullOrWhiteSpace(sfxId))
            return AudioVoiceHandle.Invalid;

        return AudioBootstrap.Sfx.PlayAttached(sfxId, transform, localOffset, request);
    }

    public AudioVoiceHandle StartAttachedLoop(string key, string sfxId, Vector3 localOffset, SfxPlayRequest request)
    {
        if (!HasAudio || string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(sfxId))
            return AudioVoiceHandle.Invalid;

        if (loopHandles.TryGetValue(key, out AudioVoiceHandle existingHandle))
        {
            if (existingHandle.IsValid)
                return existingHandle;

            loopHandles.Remove(key);
        }

        AudioVoiceHandle handle = AudioBootstrap.Sfx.PlayAttachedLoop(sfxId, transform, localOffset, request);
        if (handle.IsValid)
        {
            loopHandles.Add(key, handle);
        }

        return handle;
    }

    public AudioVoiceHandle StartWorldLoop(string key, string sfxId, Vector3 worldPosition, SfxPlayRequest request)
    {
        if (!HasAudio || string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(sfxId))
            return AudioVoiceHandle.Invalid;

        if (loopHandles.TryGetValue(key, out AudioVoiceHandle existingHandle))
        {
            if (existingHandle.IsValid)
                return existingHandle;

            loopHandles.Remove(key);
        }

        AudioVoiceHandle handle = AudioBootstrap.Sfx.PlayLoop(sfxId, worldPosition, request);
        if (handle.IsValid)
        {
            loopHandles.Add(key, handle);
        }

        return handle;
    }

    public void StopLoop(string key, float fadeOutSeconds)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        if (!loopHandles.TryGetValue(key, out AudioVoiceHandle handle))
            return;

        loopHandles.Remove(key);

        if (handle.IsValid && HasAudio)
        {
            AudioBootstrap.Sfx.Stop(handle, fadeOutSeconds);
        }
    }

    public void StartFootstepLoop(string sfxId, SfxPlayRequest request)
    {
        if (!HasAudio || string.IsNullOrWhiteSpace(sfxId))
            return;

        if (footstepLoopHandle.IsValid)
            return;

        footstepLoopHandle = AudioBootstrap.Sfx.PlayAttachedLoop(
            sfxId,
            transform,
            Vector3.zero,
            request
        );
    }

    public void StopFootstepLoop(float fadeOutSeconds)
    {
        if (!footstepLoopHandle.IsValid)
            return;

        if (HasAudio)
        {
            AudioBootstrap.Sfx.Stop(footstepLoopHandle, fadeOutSeconds);
        }

        footstepLoopHandle = AudioVoiceHandle.Invalid;
    }

    private static bool HasAudio => AudioBootstrap.Sfx != null;

    private void StopAllLoops(float fadeOutSeconds)
    {
        foreach (AudioVoiceHandle handle in loopHandles.Values)
        {
            if (handle.IsValid && HasAudio)
            {
                AudioBootstrap.Sfx.Stop(handle, fadeOutSeconds);
            }
        }

        loopHandles.Clear();
    }

    private void DispatchLegacyModules(in PlayerSfxEventContext eventContext)
    {
        if (legacyModules == null)
            return;

        PlayerSfxContext legacyContext = CreateLegacyContext(eventContext);
        for (int i = 0; i < legacyModules.Length; i++)
        {
            PlayerSfxModule module = legacyModules[i];
            if (module == null)
                continue;

            switch (eventContext.EventType)
            {
                case PlayerSfxEventType.BowDrawStarted:
                    module.OnBowDrawStarted(legacyContext);
                    break;
                case PlayerSfxEventType.BowShootReady:
                    module.OnBowShootReady(legacyContext);
                    break;
                case PlayerSfxEventType.BowShotReleased:
                    module.OnBowShotReleased(legacyContext);
                    break;
                case PlayerSfxEventType.BowShotFired:
                    module.OnBowShotFired(legacyContext);
                    break;
                case PlayerSfxEventType.BowDryReleased:
                    module.OnBowDryReleased(legacyContext);
                    break;
                case PlayerSfxEventType.DashStarted:
                    module.OnDashStarted(legacyContext);
                    break;
                case PlayerSfxEventType.DashCompleted:
                    module.OnDashCompleted(legacyContext);
                    break;
            }
        }
    }

    private static PlayerSfxContext CreateLegacyContext(in PlayerSfxEventContext context)
    {
        return new PlayerSfxContext(
            hub: context.Hub,
            transform: context.Transform,
            bow: context.Bow,
            dash: context.Dash,
            motor: context.Motor,
            rb: context.Rb);
    }
}
