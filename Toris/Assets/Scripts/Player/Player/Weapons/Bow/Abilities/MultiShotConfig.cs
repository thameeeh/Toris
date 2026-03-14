using UnityEngine;

[CreateAssetMenu(fileName = "MultiShotConfig", menuName = "Game/Abilities/Multi Shot")]
public class MultiShotConfig : PlayerAbilitySO
{
    [Header("Pattern")]
    [Min(1)] public int arrowCount = 3;
    [Min(0f)] public float totalSpreadDegrees = 20f;

    [Header("Cost")]
    [Min(0f)] public float staminaCost = 25f;

    public override void OnButtonDown(PlayerAbilityRuntime runtime, PlayerAbilityContext context)
    {
        PlayerStats playerStats = context.stats;
        PlayerBowController playerBow = context.bow;

        if (runtime == null || playerStats == null || playerBow == null)
            return;

        if (!runtime.IsReady(context))
            return;

        if (staminaCost > 0f && !playerStats.TryConsumeStamina(staminaCost))
            return;

        BowSO.ShotStats shotStats = playerBow.BuildFullyDrawnShotStats();

        playerBow.FireMultiShotVolley(shotStats, arrowCount, totalSpreadDegrees);
        runtime.StartCooldown();
    }
}