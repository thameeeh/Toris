using UnityEngine;

[CreateAssetMenu(fileName = "MultiShotConfig", menuName = "Game/Abilities/Multi Shot")]
public class MultiShotConfig : PlayerAbilitySO
{
    [Header("Pattern")]
    [Min(1)] public int arrowCount = 3;
    [Min(0f)] public float totalSpreadDegrees = 20f;

    [Header("Cost")]
    [Min(0f)] public float staminaCost = 25f;

    [Header("Animation")]
    public bool playReleaseAnimation = true;

    public override float GetStaminaCost(PlayerAbilityContext context)
    {
        return ResolveStaminaCost(staminaCost, context);
    }

    public override void OnButtonDown(PlayerAbilityRuntime runtime, PlayerAbilityContext context)
    {
        PlayerStats playerStats = context.stats;
        PlayerBowController playerBow = context.bow;

        if (runtime == null || playerStats == null || playerBow == null)
            return;

        if (!runtime.IsReady(context))
            return;

        float resolvedStaminaCost = GetStaminaCost(context);
        if (resolvedStaminaCost > 0f && !playerStats.TryConsumeStamina(resolvedStaminaCost))
            return;

        BowSO.ShotStats shotStats = playerBow.BuildFullyDrawnShotStats();

        runtime.BeginAbilityUse(context);
        playerBow.FireMultiShotVolley(shotStats, arrowCount, totalSpreadDegrees, playReleaseAnimation);
        runtime.StartCooldown(context);
    }
}
