using UnityEngine;

[CreateAssetMenu(menuName = "Audio/Player SFX Modules/Dash", fileName = "PlayerSfxModule_Dash")]
public sealed class PlayerSfxModule_Dash : PlayerSfxModule
{
    [Header("SFX IDs")]
    [SerializeField] private string dashStartSfxId = "Player_DashStart";
    [SerializeField] private string dashEndSfxId = "Player_DashEnd";

    [Header("Request")]
    [SerializeField] private bool force2D = false;

    private SfxPlayRequest MakeRequest()
    {
        var req = SfxPlayRequest.Default;
        req.force2D = force2D;
        return req;
    }

    public override void OnDashStarted(in PlayerSfxContext ctx)
    {
        if (!ctx.HasAudio) return;
        AudioBootstrap.Sfx.PlayAttached(dashStartSfxId, ctx.Transform, Vector3.zero, MakeRequest());
    }

    public override void OnDashCompleted(in PlayerSfxContext ctx)
    {
        if (!ctx.HasAudio) return;
        AudioBootstrap.Sfx.PlayAttached(dashEndSfxId, ctx.Transform, Vector3.zero, MakeRequest());
    }
}
