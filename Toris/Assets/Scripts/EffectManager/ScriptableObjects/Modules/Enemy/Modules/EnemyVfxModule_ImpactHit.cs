using UnityEngine;

[CreateAssetMenu(
    menuName = "Effects/Enemy VFX Modules/Impact Hit",
    fileName = "EnemyVfxModule_ImpactHit")]
public sealed class EnemyVfxModule_ImpactHit : EnemyVfxModule
{
    [SerializeField] private string impactEffectId = "hit_arrow_square";

    [Header("Placement")]
    [SerializeField] private bool attachToEnemy = false;
    [SerializeField] private Vector3 localOffset = Vector3.zero;
    [SerializeField] private bool alignFromAggroTarget = false;

    [Header("Parameters")]
    [SerializeField] private EffectVariant variant = default;
    [SerializeField] private bool useDamageAsMagnitude = false;
    [SerializeField] private float magnitude = 1f;
    [SerializeField] private float damageMagnitudeScale = 1f;

    public override void OnDamaged(in EnemyVfxContext ctx, float damage)
    {
        if (!ctx.HasEffects || ctx.Hub == null || ctx.Transform == null)
            return;

        if (string.IsNullOrWhiteSpace(impactEffectId))
            return;

        Quaternion worldRotation = ResolveRotation(ctx);
        float resolvedMagnitude = useDamageAsMagnitude
            ? Mathf.Max(0f, damage * damageMagnitudeScale)
            : magnitude;

        if (attachToEnemy)
        {
            Quaternion localRotation = ToLocalRotation(ctx.Transform, worldRotation);
            ctx.Hub.PlayAttachedOneShot(
                impactEffectId,
                localOffset,
                localRotation,
                variant,
                resolvedMagnitude);
            return;
        }

        Vector3 worldPosition = ctx.Transform.TransformPoint(localOffset);
        ctx.Hub.PlayOneShot(impactEffectId, worldPosition, worldRotation, variant, resolvedMagnitude);
    }

    private Quaternion ResolveRotation(in EnemyVfxContext ctx)
    {
        if (!alignFromAggroTarget || ctx.Enemy == null)
            return ctx.Transform.rotation;

        Vector2 direction = ctx.Enemy.GetDirectionToAggroTarget(ctx.Transform.position);
        return RotationFromDirection2D(direction, ctx.Transform.rotation);
    }
}
