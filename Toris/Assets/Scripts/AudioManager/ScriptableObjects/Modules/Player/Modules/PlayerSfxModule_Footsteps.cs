using UnityEngine;

[CreateAssetMenu(menuName = "Outland Haven/Audio/Legacy/Footsteps Module", fileName = "PlayerSfxModule_Footsteps")]
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

        float speed = 0f;
        if (ctx.Rb != null)
        {
#if UNITY_2022_1_OR_NEWER
            speed = ctx.Rb.linearVelocity.magnitude;
#else
            speed = ctx.Rb.velocity.magnitude;
#endif
        }

        bool isMoving = speed > minMoveThreshold;

        if (isMoving)
            ctx.Hub.StartFootstepLoop(footstepLoopSfxId, MakeRequest());
        else
            ctx.Hub.StopFootstepLoop(fadeOutSeconds);
    }
}
