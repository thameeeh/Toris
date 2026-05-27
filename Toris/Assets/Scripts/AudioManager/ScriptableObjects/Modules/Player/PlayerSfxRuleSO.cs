using UnityEngine;

public enum PlayerSfxPlaybackMode
{
    OneShot2D,
    OneShotAtPlayer,
    OneShotAtEventPosition,
    AttachedOneShot,
    StartAttachedLoop,
    StartWorldLoop,
    StopLoop
}

[CreateAssetMenu(menuName = "Outland Haven/Audio/Player SFX Rule", fileName = "PlayerSfxRule")]
public sealed class PlayerSfxRuleSO : ScriptableObject
{
    [Header("Trigger")]
    [SerializeField] private PlayerSfxEventType trigger = PlayerSfxEventType.None;
    [SerializeField] private bool filterStatusType;
    [SerializeField] private PlayerStatusEffectType statusType;
    [SerializeField] private bool filterResourceChangeReason;
    [SerializeField] private PlayerResourceChangeReason resourceChangeReason = PlayerResourceChangeReason.Unknown;
    [SerializeField] private bool ignoreRegenerationResourceChanges;
    [SerializeField] private float minimumAmount;
    [SerializeField] private float cooldownSeconds;

    [Header("Playback")]
    [SerializeField] private PlayerSfxPlaybackMode playbackMode = PlayerSfxPlaybackMode.AttachedOneShot;
    [SerializeField] private string sfxId = string.Empty;
    [SerializeField] private string loopKey = string.Empty;
    [SerializeField] private float fadeOutSeconds = 0.05f;

    [Header("Placement")]
    [SerializeField] private Vector3 offset = Vector3.zero;

    [Header("Request")]
    [SerializeField] private float volumeMultiplier = 1f;
    [SerializeField] private float pitchOffset;
    [SerializeField] private float pitchMultiplier = 1f;
    [SerializeField] private bool force2D;
    [SerializeField] private bool useEventAmountAsVolume;
    [SerializeField] private float eventAmountVolumeScale = 1f;

    [Header("Diagnostics")]
    [SerializeField] private bool debugLogging;

    public void Evaluate(in PlayerSfxEventContext context)
    {
        if (!Matches(context, out string mismatchReason))
        {
            DebugLog(context, $"Skipped: {mismatchReason}");
            return;
        }

        if (context.Hub == null || context.Transform == null || !context.HasAudio)
        {
            DebugLog(
                context,
                $"Matched but blocked: hub={context.Hub != null}, transform={context.Transform != null}, hasAudio={context.HasAudio}.");
            return;
        }

        if (playbackMode == PlayerSfxPlaybackMode.StopLoop)
        {
            if (!context.Hub.TryUseRuleCooldown(this, cooldownSeconds))
            {
                DebugLog(context, $"Matched but blocked by cooldown ({cooldownSeconds:0.###}s).");
                return;
            }

            context.Hub.StopLoop(ResolveLoopKey(), fadeOutSeconds);
            DebugLog(context, $"Stopped loop key={ResolveLoopKey()} fadeOut={fadeOutSeconds:0.###}s.");
            return;
        }

        if (string.IsNullOrWhiteSpace(sfxId))
        {
            DebugLog(context, "Matched but blocked: SFX id is empty.");
            return;
        }

        if (!context.Hub.TryUseRuleCooldown(this, cooldownSeconds))
        {
            DebugLog(context, $"Matched but blocked by cooldown ({cooldownSeconds:0.###}s).");
            return;
        }

        SfxPlayRequest request = MakeRequest(context);
        AudioVoiceHandle handle;

        switch (playbackMode)
        {
            case PlayerSfxPlaybackMode.OneShot2D:
                handle = PlayOneShot2D(context, request);
                break;
            case PlayerSfxPlaybackMode.OneShotAtEventPosition:
                handle = context.Hub.PlayOneShot(sfxId, context.WorldPosition + offset, request);
                break;
            case PlayerSfxPlaybackMode.AttachedOneShot:
                handle = context.Hub.PlayAttachedOneShot(sfxId, offset, request);
                break;
            case PlayerSfxPlaybackMode.StartAttachedLoop:
                handle = context.Hub.StartAttachedLoop(ResolveLoopKey(), sfxId, offset, request);
                break;
            case PlayerSfxPlaybackMode.StartWorldLoop:
                handle = context.Hub.StartWorldLoop(ResolveLoopKey(), sfxId, context.WorldPosition + offset, request);
                break;
            default:
                handle = context.Hub.PlayOneShot(sfxId, context.Transform.TransformPoint(offset), request);
                break;
        }

        DebugLog(
            context,
            handle.IsValid
                ? $"Played id={sfxId} mode={playbackMode} handle={handle}."
                : $"Matched and requested id={sfxId} mode={playbackMode}, but AudioManager returned an invalid handle.");
    }

    private bool Matches(in PlayerSfxEventContext context, out string reason)
    {
        if (trigger == PlayerSfxEventType.None)
        {
            reason = "trigger is None";
            return false;
        }

        if (context.EventType != trigger)
        {
            reason = $"event is {context.EventType}, trigger is {trigger}";
            return false;
        }

        if (filterStatusType && (!context.HasStatusType || context.StatusType != statusType))
        {
            reason = $"status filter failed. hasStatus={context.HasStatusType}, eventStatus={context.StatusType}, required={statusType}";
            return false;
        }

        if (filterResourceChangeReason && context.ResourceChangeReason != resourceChangeReason)
        {
            reason = $"resource reason is {context.ResourceChangeReason}, required={resourceChangeReason}";
            return false;
        }

        if (ignoreRegenerationResourceChanges &&
            context.ResourceChangeReason == PlayerResourceChangeReason.Regeneration)
        {
            reason = "resource reason is Regeneration";
            return false;
        }

        if (minimumAmount > 0f && Mathf.Abs(context.Amount) < minimumAmount)
        {
            reason = $"amount {Mathf.Abs(context.Amount):0.###} is below minimum {minimumAmount:0.###}";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    private SfxPlayRequest MakeRequest(in PlayerSfxEventContext context)
    {
        SfxPlayRequest request = SfxPlayRequest.Default;
        request.volumeMultiplier = ResolveVolume(context);
        request.pitchOffset = pitchOffset;
        request.pitchMultiplier = Mathf.Max(0f, pitchMultiplier);
        request.force2D = force2D || playbackMode == PlayerSfxPlaybackMode.OneShot2D;
        return request;
    }

    private AudioVoiceHandle PlayOneShot2D(in PlayerSfxEventContext context, SfxPlayRequest request)
    {
        request.explicitWorldPosition = context.Transform.position + offset;
        return context.Hub.PlayOneShot(sfxId, request.explicitWorldPosition.Value, request);
    }

    private float ResolveVolume(in PlayerSfxEventContext context)
    {
        float resolvedVolume = Mathf.Max(0f, volumeMultiplier);
        if (useEventAmountAsVolume)
        {
            resolvedVolume *= Mathf.Max(0f, Mathf.Abs(context.Amount) * eventAmountVolumeScale);
        }

        return resolvedVolume;
    }

    private string ResolveLoopKey()
    {
        return string.IsNullOrWhiteSpace(loopKey)
            ? name
            : loopKey;
    }

    private void DebugLog(in PlayerSfxEventContext context, string message)
    {
#if UNITY_EDITOR
        if (!debugLogging)
            return;

        Debug.Log(
            $"[PlayerSfxRuleSO:{name}] {message} event={context.EventType}, reason={context.ResourceChangeReason}, amount={context.Amount:0.###}, world={context.WorldPosition}",
            this);
#endif
    }
}
