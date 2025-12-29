using UnityEngine;

[CreateAssetMenu(menuName = "Audio/Player SFX Modules/Footsteps", fileName = "PlayerSfxModule_Footsteps")]
public sealed class PlayerSfxModule_Footsteps : PlayerSfxModule
{
    [Header("SFX ID (loop)")]
    [SerializeField] private string footstepLoopSfxId = "Player_Footstep";

    [Header("Movement Detection")]
    [SerializeField] private float minMoveThreshold = 0.10f;

    [Header("Stop")]
    [SerializeField] private float fadeOutSeconds = 0.08f;

    [Header("Request")]
    [SerializeField] private bool force2D = false;

    private SfxPlayRequest MakeRequest()
    {
        var req = SfxPlayRequest.Default;
        req.force2D = force2D;
        return req;
    }

    public override void Tick(in PlayerSfxContext ctx, float unscaledDeltaTime)
    {
        if (!ctx.HasAudio) return;

        bool isDashing = ctx.Motor != null && ctx.Motor.isDashing;
        if (isDashing)
        {
            ctx.Hub.StopFootstepLoop(fadeOutSeconds);
            return;
        }

        float speed = ctx.Rb != null ? ctx.Rb.linearVelocity.magnitude : 0f;
        bool isMoving = speed > minMoveThreshold;

        if (isMoving)
            ctx.Hub.StartFootstepLoop(footstepLoopSfxId, MakeRequest());
        else
            ctx.Hub.StopFootstepLoop(fadeOutSeconds);
    }
}
