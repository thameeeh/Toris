using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ChainShotConfig", menuName = "Game/Abilities/Chain Shot")]
public class ChainShotConfig : PlayerAbilitySO
{
    [Header("Damage")]
    [Tooltip("Index 0 is the first hit, index 1 is the first chain hop, and so on.")]
    public List<float> damageMultipliers = new List<float> { 1f, 0.5f, 0.3f };

    [Header("Initial Shot")]
    [Min(0.1f)] public float initialProjectileSpeedMultiplier = 1f;

    [Header("Chain")]
    [Min(0.1f)] public float chainSearchRadius = 5f;
    [Min(0.1f)] public float chainProjectileSpeed = 18f;
    [Min(0.05f)] public float chainProjectileLifetime = 1.5f;
    [Min(0f)] public float chainJumpDelay = 0.05f;
    public bool playChainImpactEffect = true;

    [Header("Cost")]
    [Min(0f)] public float staminaCost = 20f;

    [Header("Animation")]
    public bool playReleaseAnimation = true;

    public override void OnButtonDown(PlayerAbilityRuntime runtime, PlayerAbilityContext context)
    {
        PlayerStats playerStats = context.stats;
        PlayerBowController playerBow = context.bow;

        if (runtime == null || playerStats == null || playerBow == null)
            return;

        if (!runtime.IsReady(context))
            return;

        if (damageMultipliers == null || damageMultipliers.Count == 0)
            return;

        if (staminaCost > 0f && !playerStats.TryConsumeStamina(staminaCost))
            return;

        BowSO.ShotStats baseShotStats = playerBow.BuildFullyDrawnShotStats();
        float baseDamage = baseShotStats.damage;

        baseShotStats.damage = baseDamage * damageMultipliers[0];
        baseShotStats.speed *= initialProjectileSpeedMultiplier;

        runtime.BeginAbilityUse(context);
        playerBow.FireChainShot(
            baseShotStats,
            new PlayerBowController.ChainShotSettings
            {
                baseDamage = baseDamage,
                damageMultipliers = damageMultipliers.ToArray(),
                chainSearchRadius = chainSearchRadius,
                chainProjectileSpeed = chainProjectileSpeed,
                chainProjectileLifetime = chainProjectileLifetime,
                chainJumpDelay = chainJumpDelay,
                playImpactEffect = playChainImpactEffect
            },
            playReleaseAnimation);
        runtime.StartCooldown();
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();

        if (damageMultipliers == null)
            damageMultipliers = new List<float>();

        if (damageMultipliers.Count == 0)
            damageMultipliers.Add(1f);
    }
#endif
}
