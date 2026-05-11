using UnityEngine;

public enum PlayerVfxPlaybackMode
{
    OneShotAtPlayer,
    OneShotAtEventPosition,
    AttachedOneShot,
    StartPersistentAttached,
    ReleasePersistent
}

public enum PlayerVfxRotationMode
{
    Identity,
    PlayerRotation,
    FacingDirection,
    EventDirection,
    BowAimDirection
}

[CreateAssetMenu(menuName = "Effects/Player VFX Rule", fileName = "PlayerVfxRule")]
public sealed class PlayerVfxRuleSO : ScriptableObject
{
    private const float MIN_DIRECTION_SQR_MAGNITUDE = 0.0001f;

    [Header("Trigger")]
    [SerializeField] private PlayerVfxEventType trigger = PlayerVfxEventType.None;
    [SerializeField] private bool filterStatusType;
    [SerializeField] private PlayerStatusEffectType statusType;
    [SerializeField] private float minimumAmount;
    [SerializeField] private float cooldownSeconds;

    [Header("Playback")]
    [SerializeField] private PlayerVfxPlaybackMode playbackMode = PlayerVfxPlaybackMode.OneShotAtPlayer;
    [SerializeField] private string effectId = string.Empty;
    [SerializeField] private string persistentKey = string.Empty;

    [Header("Placement")]
    [SerializeField] private Vector3 offset = Vector3.zero;
    [SerializeField] private PlayerVfxRotationMode rotationMode = PlayerVfxRotationMode.PlayerRotation;

    [Header("Parameters")]
    [SerializeField] private EffectVariant variant = default;
    [SerializeField] private float magnitude = 1f;
    [SerializeField] private bool useEventAmountAsMagnitude;
    [SerializeField] private float eventAmountMagnitudeScale = 1f;

    public void Evaluate(in PlayerVfxEventContext context)
    {
        if (!Matches(context))
            return;

        if (context.Hub == null || context.Transform == null || !context.HasEffects)
            return;

        if (playbackMode == PlayerVfxPlaybackMode.ReleasePersistent)
        {
            if (!context.Hub.TryUseRuleCooldown(this, cooldownSeconds))
                return;

            context.Hub.ReleasePersistentEffect(ResolvePersistentKey());
            return;
        }

        if (string.IsNullOrWhiteSpace(effectId))
            return;

        if (!context.Hub.TryUseRuleCooldown(this, cooldownSeconds))
            return;

        float resolvedMagnitude = ResolveMagnitude(context);

        switch (playbackMode)
        {
            case PlayerVfxPlaybackMode.OneShotAtEventPosition:
                PlayOneShotAtEventPosition(context, resolvedMagnitude);
                break;
            case PlayerVfxPlaybackMode.AttachedOneShot:
                PlayAttachedOneShot(context, resolvedMagnitude);
                break;
            case PlayerVfxPlaybackMode.StartPersistentAttached:
                StartPersistent(context, resolvedMagnitude);
                break;
            default:
                PlayOneShotAtPlayer(context, resolvedMagnitude);
                break;
        }
    }

    private bool Matches(in PlayerVfxEventContext context)
    {
        if (trigger == PlayerVfxEventType.None || context.EventType != trigger)
            return false;

        if (filterStatusType && (!context.HasStatusType || context.StatusType != statusType))
            return false;

        if (minimumAmount > 0f && Mathf.Abs(context.Amount) < minimumAmount)
            return false;

        return true;
    }

    private void PlayOneShotAtPlayer(in PlayerVfxEventContext context, float resolvedMagnitude)
    {
        Quaternion worldRotation = ResolveWorldRotation(context);
        Vector3 worldPosition = context.Transform.TransformPoint(offset);
        context.Hub.PlayOneShot(effectId, worldPosition, worldRotation, variant, resolvedMagnitude);
    }

    private void PlayOneShotAtEventPosition(in PlayerVfxEventContext context, float resolvedMagnitude)
    {
        Quaternion worldRotation = ResolveWorldRotation(context);
        Vector3 worldPosition = context.WorldPosition + offset;
        context.Hub.PlayOneShot(effectId, worldPosition, worldRotation, variant, resolvedMagnitude);
    }

    private void PlayAttachedOneShot(in PlayerVfxEventContext context, float resolvedMagnitude)
    {
        Quaternion localRotation = ToLocalRotation(context.Transform, ResolveWorldRotation(context));
        context.Hub.PlayAttachedOneShot(effectId, offset, localRotation, variant, resolvedMagnitude);
    }

    private void StartPersistent(in PlayerVfxEventContext context, float resolvedMagnitude)
    {
        Quaternion localRotation = ToLocalRotation(context.Transform, ResolveWorldRotation(context));
        context.Hub.StartPersistentEffect(
            ResolvePersistentKey(),
            effectId,
            offset,
            localRotation,
            variant,
            resolvedMagnitude);
    }

    private float ResolveMagnitude(in PlayerVfxEventContext context)
    {
        if (!useEventAmountAsMagnitude)
            return Mathf.Max(0f, magnitude);

        return Mathf.Max(0f, Mathf.Abs(context.Amount) * eventAmountMagnitudeScale);
    }

    private string ResolvePersistentKey()
    {
        return string.IsNullOrWhiteSpace(persistentKey)
            ? name
            : persistentKey;
    }

    private Quaternion ResolveWorldRotation(in PlayerVfxEventContext context)
    {
        switch (rotationMode)
        {
            case PlayerVfxRotationMode.Identity:
                return Quaternion.identity;
            case PlayerVfxRotationMode.FacingDirection:
                return RotationFromDirection2D(context.CurrentFacingDirection, context.Transform.rotation);
            case PlayerVfxRotationMode.EventDirection:
                return RotationFromDirection2D(context.Direction, context.Transform.rotation);
            case PlayerVfxRotationMode.BowAimDirection:
                return RotationFromDirection2D(ResolveBowAimDirection(context), context.Transform.rotation);
            default:
                return context.Transform.rotation;
        }
    }

    private static Vector2 ResolveBowAimDirection(in PlayerVfxEventContext context)
    {
        if (context.Bow != null)
        {
            Vector2 aimDirection = context.Bow.CurrentAimDirection;
            if (aimDirection.sqrMagnitude > MIN_DIRECTION_SQR_MAGNITUDE)
                return aimDirection.normalized;
        }

        return context.CurrentFacingDirection;
    }

    private static Quaternion RotationFromDirection2D(Vector2 direction, Quaternion fallback)
    {
        if (direction.sqrMagnitude < MIN_DIRECTION_SQR_MAGNITUDE)
            return fallback;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        return Quaternion.Euler(0f, 0f, angle);
    }

    private static Quaternion ToLocalRotation(Transform parent, Quaternion worldRotation)
    {
        return parent != null
            ? Quaternion.Inverse(parent.rotation) * worldRotation
            : worldRotation;
    }
}
