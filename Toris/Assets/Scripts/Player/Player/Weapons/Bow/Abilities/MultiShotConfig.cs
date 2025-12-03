using UnityEngine;

[CreateAssetMenu(fileName = "MultiShotConfig", menuName = "Game/Abilities/Multi Shot")]
public class MultiShotConfig : PlayerAbilitySO
{
    [Header("Pattern")]
    [Min(1)] public int arrowCount = 3;
    [Min(0f)] public float totalSpreadDegrees = 20f;

    [Header("Cost")]
    [Min(0f)] public float staminaCost = 25f;

    public override void OnButtonDown(PlayerAbilityContext context)
    {
        var stats = context.stats;
        var bow = context.bow;

        if (stats == null || bow == null)
        {
            //Debug.LogWarning("[MultiShot] Missing stats or bow in context, aborting.");
            return;
        }

        // Multishot is always unlocked unless you override IsUnlocked, but let's log anyway.
        if (!IsUnlocked(context))
        {
            //Debug.Log("[MultiShot] IsUnlocked returned false, ability locked.");
            return;
        }

        if (isOnCooldown)
        {
            //Debug.Log("[MultiShot] Ability on cooldown, cannot fire.");
            return;
        }

        if (staminaCost > 0f && !stats.TryConsumeStamina(staminaCost))
        {
            //Debug.Log("[MultiShot] Not enough stamina. Required: " + staminaCost);
            return;
        }

        BowSO bowConfig = bow.BowConfig;
        BowSO.ShotStats shotStats;

        if (bowConfig != null)
        {
            shotStats = bowConfig.BuildShotStats(bowConfig.maxDrawTime, 0f);
        }
        else
        {
            //Debug.LogWarning("[MultiShot] BowConfig is null on bow, using fallback ShotStats.");
            shotStats = new BowSO.ShotStats
            {
                power = 1f,
                speed = 10f,
                damage = 10f,
                spreadDeg = 0f
            };
        }

        //Debug.Log($"[MultiShot] Firing volley: count={arrowCount}, spread={totalSpreadDegrees}");

        bow.FireMultiShotVolley(shotStats, arrowCount, totalSpreadDegrees);

        if (cooldownSeconds > 0f)
        {
            StartCooldown();
            //Debug.Log($"[MultiShot] Starting cooldown: {cooldownSeconds} seconds.");
        }
    }
}
