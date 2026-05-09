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

[CreateAssetMenu(menuName = "Audio/Player SFX Rule", fileName = "PlayerSfxRule")]
public sealed class PlayerSfxRuleSO : ScriptableObject
{
    [Header("Trigger")]
    [SerializeField] private PlayerSfxEventType trigger = PlayerSfxEventType.None;
    [SerializeField] private bool filterStatusType;
    [SerializeField] private PlayerStatusEffectType statusType;
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

    public void Evaluate(in PlayerSfxEventContext context)
    {
        if (!Matches(context))
            return;

        if (context.Hub == null || context.Transform == null || !context.HasAudio)
            return;

        if (playbackMode == PlayerSfxPlaybackMode.StopLoop)
        {
            if (!context.Hub.TryUseRuleCooldown(this, cooldownSeconds))
                return;

            context.Hub.StopLoop(ResolveLoopKey(), fadeOutSeconds);
            return;
        }

        if (string.IsNullOrWhiteSpace(sfxId))
            return;

        if (!context.Hub.TryUseRuleCooldown(this, cooldownSeconds))
            return;

        SfxPlayRequest request = MakeRequest(context);

        switch (playbackMode)
        {
            case PlayerSfxPlaybackMode.OneShot2D:
                PlayOneShot2D(context, request);
                break;
            case PlayerSfxPlaybackMode.OneShotAtEventPosition:
                context.Hub.PlayOneShot(sfxId, context.WorldPosition + offset, request);
                break;
            case PlayerSfxPlaybackMode.AttachedOneShot:
                context.Hub.PlayAttachedOneShot(sfxId, offset, request);
                break;
            case PlayerSfxPlaybackMode.StartAttachedLoop:
                context.Hub.StartAttachedLoop(ResolveLoopKey(), sfxId, offset, request);
                break;
            case PlayerSfxPlaybackMode.StartWorldLoop:
                context.Hub.StartWorldLoop(ResolveLoopKey(), sfxId, context.WorldPosition + offset, request);
                break;
            default:
                context.Hub.PlayOneShot(sfxId, context.Transform.TransformPoint(offset), request);
                break;
        }
    }

    private bool Matches(in PlayerSfxEventContext context)
    {
        if (trigger == PlayerSfxEventType.None || context.EventType != trigger)
            return false;

        if (filterStatusType && (!context.HasStatusType || context.StatusType != statusType))
            return false;

        if (minimumAmount > 0f && Mathf.Abs(context.Amount) < minimumAmount)
            return false;

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

    private void PlayOneShot2D(in PlayerSfxEventContext context, SfxPlayRequest request)
    {
        request.explicitWorldPosition = context.Transform.position + offset;
        context.Hub.PlayOneShot(sfxId, request.explicitWorldPosition.Value, request);
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
}
