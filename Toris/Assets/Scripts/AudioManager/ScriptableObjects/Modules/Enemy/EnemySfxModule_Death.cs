using UnityEngine;

[CreateAssetMenu(
    menuName = "Audio/Enemy SFX Modules/Death",
    fileName = "EnemySfxModule_Death")]
public sealed class EnemySfxModule_Death : EnemySfxModule
{
    [SerializeField] private string deathSfxId = string.Empty;
    [SerializeField] private Vector3 localOffset = Vector3.zero;
    [SerializeField] private bool force2D = false;

    public override void OnDied(in EnemySfxContext ctx, Enemy enemy)
    {
        if (!ctx.HasAudio || ctx.Transform == null)
            return;

        if (string.IsNullOrWhiteSpace(deathSfxId))
            return;

        var request = SfxPlayRequest.Default;
        request.force2D = force2D;

        Vector3 worldPosition = ctx.Transform.TransformPoint(localOffset);

        // SFX-only hook: enemy death audio plays after Enemy.Died is raised.
        // It must not affect health, loot, quests, pooling, or death-state transitions.
        AudioBootstrap.Sfx.PlayAt(deathSfxId, worldPosition, request);
    }
}
