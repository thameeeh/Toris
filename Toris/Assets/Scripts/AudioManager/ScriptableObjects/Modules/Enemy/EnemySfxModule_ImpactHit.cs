using UnityEngine;

[CreateAssetMenu(
    menuName = "Audio/Enemy SFX Modules/Impact Hit",
    fileName = "EnemySfxModule_ImpactHit")]
public sealed class EnemySfxModule_ImpactHit : EnemySfxModule
{
    [SerializeField] private string impactSfxId = "Enemy_ImpactHit";
    [SerializeField] private bool force2D = false;

    public override void OnDamaged(in EnemySfxContext ctx, float damage)
    {
        if (!ctx.HasAudio) return;

        var req = SfxPlayRequest.Default;
        req.force2D = force2D;

        AudioBootstrap.Sfx.PlayAttached(
            impactSfxId,
            ctx.Transform,
            Vector3.zero,
            req
        );
    }
}
