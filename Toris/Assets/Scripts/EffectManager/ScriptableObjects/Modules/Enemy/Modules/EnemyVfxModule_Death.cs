using UnityEngine;

[CreateAssetMenu(
    menuName = "Outland Haven/Effects/Enemy VFX Modules/Death",
    fileName = "EnemyVfxModule_Death")]
public sealed class EnemyVfxModule_Death : EnemyVfxModule
{
    [SerializeField] private string deathEffectId = string.Empty;

    [Header("Placement")]
    [SerializeField] private Vector3 localOffset = Vector3.zero;

    [Header("Parameters")]
    [SerializeField] private EffectVariant variant = default;
    [SerializeField] private float magnitude = 1f;

    public override void OnDied(in EnemyVfxContext ctx, Enemy enemy)
    {
        if (!ctx.HasEffects || ctx.Hub == null || ctx.Transform == null)
            return;

        if (string.IsNullOrWhiteSpace(deathEffectId))
            return;

        Vector3 worldPosition = ctx.Transform.TransformPoint(localOffset);
        ctx.Hub.PlayOneShot(deathEffectId, worldPosition, ctx.Transform.rotation, variant, magnitude);
    }
}
