using UnityEngine;

[CreateAssetMenu(fileName = "RambowBowConfig", menuName = "Game/Abilities/Rambow")]
public class RambowBowConfig : PlayerAbilitySO
{
    private const string ProjectileDebugSource = "Rambow";
    private const float DefaultProjectileLifetime = 3f;
    private const float MinimumProjectileLifetime = 0.05f;

    [Header("Unlock Requirements")]
    [Min(0)] public int killsRequired = 30;

    [Header("Firing Behaviour")]
    [Min(0.1f)] public float shotsPerSecond = 8f;
    [Min(0f)] public float spreadDegrees = 6f;
    [Min(0f)] public float damagePerShot = 8f;
    [Min(0.1f)] public float speedPerShot = 12f;
    [Min(0.05f)] public float projectileLifetime = 3f;

    [Header("Cost")]
    [Min(0f)] public float initialStaminaCost = 10f;
    [Min(0f)] public float staminaPerShot = 2f;

    [Header("Misc")]
    [Min(0f)] public float maxDuration = 0f;

    [Header("Animation")]
    public bool playReleaseAnimation = true;

    public override PlayerAbilityRuntime CreateRuntime()
    {
        return new RamboBowRuntime();
    }
    public override bool IsUnlocked(PlayerAbilityContext context)
    {
        return base.IsUnlocked(context);
    }

    public override void OnButtonDown(PlayerAbilityRuntime runtime, PlayerAbilityContext context)
    {
        PlayerStats playerStats = context.stats;
        PlayerBowController playerBow = context.bow;
        RamboBowRuntime ramboRuntime = runtime as RamboBowRuntime;

        if (playerStats == null || playerBow == null || ramboRuntime == null)
            return;

        LogRambow(playerBow, "Button down.");
        ramboRuntime.SetHeld(true);

        if (!ramboRuntime.IsReady(context))
        {
            LogRambow(playerBow, $"Activation blocked. cooldownRemaining={ramboRuntime.CooldownRemaining:F2}");
            return;
        }

        if (initialStaminaCost > 0f && !playerStats.TryConsumeStamina(initialStaminaCost))
        {
            LogRambow(playerBow, $"Activation blocked. Missing initial stamina cost={initialStaminaCost:F2}");
            return;
        }

        ramboRuntime.Activate();
        ramboRuntime.BeginAbilityUse(context);
        FireRambowShot(context, playReleaseAnimation);
        ramboRuntime.ScheduleNextShot(shotsPerSecond);
        ramboRuntime.StartCooldown();
        LogRambow(playerBow, $"Activated. shotsPerSecond={shotsPerSecond:F2} nextShotTime={ramboRuntime.NextShotTime:F3}");
    }

    public override void OnButtonUp(PlayerAbilityRuntime runtime, PlayerAbilityContext context)
    {
        if (runtime is RamboBowRuntime ramboRuntime)
        {
            LogRambow(context.bow, "Button up. Deactivating.");
            ramboRuntime.Deactivate();
        }
    }

    public override void Tick(PlayerAbilityRuntime runtime, PlayerAbilityContext context)
    {
        PlayerStats playerStats = context.stats;
        PlayerBowController playerBow = context.bow;
        RamboBowRuntime ramboRuntime = runtime as RamboBowRuntime;

        if (playerStats == null || playerBow == null || ramboRuntime == null)
            return;

        if (!ramboRuntime.IsActive)
            return;

        if (!ramboRuntime.IsHeld)
        {
            LogRambow(playerBow, "Deactivated because button is no longer held.");
            ramboRuntime.Deactivate();
            return;
        }

        if (ramboRuntime.HasReachedMaxDuration(maxDuration))
        {
            LogRambow(playerBow, $"Deactivated by maxDuration={maxDuration:F2}.");
            ramboRuntime.Deactivate();
            return;
        }

        if (!ramboRuntime.CanFireNow())
            return;

        if (staminaPerShot > 0f && !playerStats.TryConsumeStamina(staminaPerShot))
        {
            LogRambow(playerBow, $"Deactivated because staminaPerShot={staminaPerShot:F2} could not be paid.");
            ramboRuntime.Deactivate();
            return;
        }

        FireRambowShot(context, false);
        ramboRuntime.ScheduleNextShot(shotsPerSecond);
        LogRambow(playerBow, $"Shot scheduled. nextShotTime={ramboRuntime.NextShotTime:F3}");
    }

    private void FireRambowShot(PlayerAbilityContext context, bool requestReleaseAnimation)
    {
        PlayerBowController playerBow = context.bow;
        if (playerBow == null)
            return;

        BowSO.ShotStats shotStats = new BowSO.ShotStats
        {
            power = 1f,
            speed = speedPerShot,
            damage = damagePerShot,
            spreadDeg = spreadDegrees
        };

        float resolvedProjectileLifetime = ResolveProjectileLifetime();
        LogRambow(playerBow,
            $"Firing shot. speed={shotStats.speed:F2} damage={shotStats.damage:F2} spread={shotStats.spreadDeg:F2} lifetime={resolvedProjectileLifetime:F2} releaseAnimation={requestReleaseAnimation}");

        playerBow.FireArrow(shotStats, requestReleaseAnimation, ProjectileDebugSource, resolvedProjectileLifetime);
    }

    private float ResolveProjectileLifetime()
    {
        return projectileLifetime > 0f
            ? Mathf.Max(MinimumProjectileLifetime, projectileLifetime)
            : DefaultProjectileLifetime;
    }

    private static void LogRambow(Object context, string message)
    {
        PlayerShootDebug.Log(context, ProjectileDebugSource, message);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (projectileLifetime <= 0f)
            projectileLifetime = DefaultProjectileLifetime;
    }
#endif
}
