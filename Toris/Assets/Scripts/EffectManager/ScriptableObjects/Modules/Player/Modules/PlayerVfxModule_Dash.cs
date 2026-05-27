using UnityEngine;

[CreateAssetMenu(menuName = "Outland Haven/VFX/Legacy/Dash Module", fileName = "PlayerVfxModule_Dash")]
public sealed class PlayerVfxModule_Dash : PlayerVfxModule
{
    [Header("Effect IDs")]
    [SerializeField] private string dashStartEffectId = string.Empty;
    [SerializeField] private string dashEndEffectId = string.Empty;

    [Header("Placement")]
    [SerializeField] private bool attachToPlayer = false;
    [SerializeField] private Vector3 startLocalOffset = Vector3.zero;
    [SerializeField] private Vector3 endLocalOffset = Vector3.zero;
    [SerializeField] private bool alignToDashDirection = true;

    [Header("Parameters")]
    [SerializeField] private EffectVariant variant = default;
    [SerializeField] private float magnitude = 1f;

    public override void OnDashStarted(in PlayerVfxContext ctx, Vector2 direction)
    {
        PlayDashEffect(ctx, dashStartEffectId, startLocalOffset, direction);
    }

    public override void OnDashCompleted(in PlayerVfxContext ctx)
    {
        PlayDashEffect(ctx, dashEndEffectId, endLocalOffset, ctx.CurrentFacingDirection);
    }

    private void PlayDashEffect(
        in PlayerVfxContext ctx,
        string effectId,
        Vector3 localOffset,
        Vector2 direction)
    {
        if (!ctx.HasEffects || ctx.Hub == null || ctx.Transform == null)
            return;

        if (string.IsNullOrWhiteSpace(effectId))
            return;

        Quaternion worldRotation = alignToDashDirection
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
}
