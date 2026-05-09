using UnityEngine;

[CreateAssetMenu(menuName = "Effects/Legacy/Player VFX Modules/Bow", fileName = "PlayerVfxModule_Bow")]
public sealed class PlayerVfxModule_Bow : PlayerVfxModule
{
    [Header("Effect IDs")]
    [SerializeField] private string drawStartEffectId = string.Empty;
    [SerializeField] private string shootReadyEffectId = string.Empty;
    [SerializeField] private string shotFiredEffectId = string.Empty;
    [SerializeField] private string dryReleaseEffectId = string.Empty;

    [Header("Placement")]
    [SerializeField] private bool attachToPlayer = true;
    [SerializeField] private Vector3 localOffset = Vector3.zero;
    [SerializeField] private bool alignToAimDirection = true;

    [Header("Parameters")]
    [SerializeField] private EffectVariant variant = default;
    [SerializeField] private float magnitude = 1f;

    public override void OnBowDrawStarted(in PlayerVfxContext ctx)
    {
        PlayBowEffect(ctx, drawStartEffectId);
    }

    public override void OnBowShootReady(in PlayerVfxContext ctx)
    {
        PlayBowEffect(ctx, shootReadyEffectId);
    }

    public override void OnBowShotFired(in PlayerVfxContext ctx)
    {
        PlayBowEffect(ctx, shotFiredEffectId);
    }

    public override void OnBowDryReleased(in PlayerVfxContext ctx)
    {
        PlayBowEffect(ctx, dryReleaseEffectId);
    }

    private void PlayBowEffect(in PlayerVfxContext ctx, string effectId)
    {
        if (!ctx.HasEffects || ctx.Hub == null || ctx.Transform == null)
            return;

        if (string.IsNullOrWhiteSpace(effectId))
            return;

        Vector2 direction = ResolveAimDirection(ctx);
        Quaternion worldRotation = alignToAimDirection
            ? RotationFromDirection2D(direction, ctx.Transform.rotation)
            : ctx.Transform.rotation;

        if (attachToPlayer)
        {
            Quaternion localRotation = ToLocalRotation(ctx.Transform, worldRotation);
            ctx.Hub.PlayAttachedOneShot(effectId, localOffset, localRotation, variant, magnitude);
            return;
        }

        Vector3 worldPosition = ctx.Transform.TransformPoint(localOffset);
        ctx.Hub.PlayOneShot(effectId, worldPosition, worldRotation, variant, magnitude);
    }

    private static Vector2 ResolveAimDirection(in PlayerVfxContext ctx)
    {
        if (ctx.Bow != null)
        {
            Vector2 aimDirection = ctx.Bow.CurrentAimDirection;
            if (aimDirection.sqrMagnitude > 0.0001f)
                return aimDirection;
        }

        return ctx.CurrentFacingDirection;
    }
}
