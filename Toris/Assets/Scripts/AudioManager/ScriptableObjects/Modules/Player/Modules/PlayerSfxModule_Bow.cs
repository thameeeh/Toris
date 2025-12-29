using UnityEngine;

[CreateAssetMenu(menuName = "Audio/Player SFX Modules/Bow", fileName = "PlayerSfxModule_Bow")]
public sealed class PlayerSfxModule_Bow : PlayerSfxModule
{
    [Header("SFX IDs")]
    [SerializeField] private string pullStartSfxId = "Bow_PullStart";
    [SerializeField] private string releaseSfxId = "Bow_Release";
    [SerializeField] private string dryReleaseSfxId = "Bow_DryRelease";

    [Header("Request")]
    [SerializeField] private bool force2D = false;

    private SfxPlayRequest MakeRequest()
    {
        var req = SfxPlayRequest.Default;
        req.force2D = force2D;
        return req;
    }

    public override void OnBowDrawStarted(in PlayerSfxContext ctx)
    {
        if (!ctx.HasAudio) return;
        AudioBootstrap.Sfx.PlayAttached(pullStartSfxId, ctx.Transform, Vector3.zero, MakeRequest());
    }

    public override void OnBowShotFired(in PlayerSfxContext ctx)
    {
        if (!ctx.HasAudio) return;
        AudioBootstrap.Sfx.PlayAttached(releaseSfxId, ctx.Transform, Vector3.zero, MakeRequest());
    }

    public override void OnBowDryReleased(in PlayerSfxContext ctx)
    {
        if (!ctx.HasAudio) return;
        AudioBootstrap.Sfx.PlayAttached(dryReleaseSfxId, ctx.Transform, Vector3.zero, MakeRequest());
    }
}
