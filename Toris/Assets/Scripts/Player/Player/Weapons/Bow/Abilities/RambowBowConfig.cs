using UnityEngine;

[CreateAssetMenu(fileName = "RambowBowConfig", menuName = "Game/Abilities/Rambow")]
public class RambowBowConfig : PlayerAbilitySO
{
    [Header("Unlock Requirements")]
    [Min(0)] public int killsRequired = 30;

    [Header("Firing Behaviour")]
    [Min(0.1f)] public float shotsPerSecond = 8f;
    [Min(0f)] public float spreadDegrees = 6f;
    [Min(0f)] public float damagePerShot = 8f;
    [Min(0.1f)] public float speedPerShot = 12f;

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
        bool unlocked = true;
        return unlocked;
    }

    public override void OnButtonDown(PlayerAbilityRuntime runtime, PlayerAbilityContext context)
    {
        PlayerStats playerStats = context.stats;
        PlayerBowController playerBow = context.bow;
        RamboBowRuntime ramboRuntime = runtime as RamboBowRuntime;

        if (playerStats == null || playerBow == null || ramboRuntime == null)
            return;

        ramboRuntime.SetHeld(true);

        if (!ramboRuntime.IsReady(context))
            return;

        if (initialStaminaCost > 0f && !playerStats.TryConsumeStamina(initialStaminaCost))
            return;

        ramboRuntime.Activate();
        ramboRuntime.BeginAbilityUse(context);
        FireRambowShot(context);
        ramboRuntime.ScheduleNextShot(shotsPerSecond);
        ramboRuntime.StartCooldown();
    }

    public override void OnButtonUp(PlayerAbilityRuntime runtime, PlayerAbilityContext context)
    {
        if (runtime is RamboBowRuntime ramboRuntime)
        {
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
            ramboRuntime.Deactivate();
            return;
        }

        if (ramboRuntime.HasReachedMaxDuration(maxDuration))
        {
            ramboRuntime.Deactivate();
            return;
        }

        if (!ramboRuntime.CanFireNow())
            return;

        if (staminaPerShot > 0f && !playerStats.TryConsumeStamina(staminaPerShot))
        {
            ramboRuntime.Deactivate();
            return;
        }

        FireRambowShot(context);
        ramboRuntime.ScheduleNextShot(shotsPerSecond);
    }

    private void FireRambowShot(PlayerAbilityContext context)
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

        playerBow.FireArrow(shotStats, playReleaseAnimation);
    }
}
