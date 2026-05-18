using UnityEngine;

[CreateAssetMenu(fileName = "DecoyTotemConfig", menuName = "Game/Abilities/Decoy Totem")]
public class DecoyTotemConfig : PlayerAbilitySO
{
    private const float MinDirectionSqrMagnitude = 0.0001f;

    [Header("Targeting")]
    [Min(0f)] public float maxPlacementRange = 8f;

    [Header("Totem")]
    public DecoyTotem totemPrefab;
    [Min(1f)] public float maxHealth = 50f;
    [Min(0.05f)] public float duration = 6f;
    [Min(0.1f)] public float retargetRadius = 6f;
    [Min(0.05f)] public float retargetInterval = 0.25f;
    [Min(0.05f)] public float targetColliderRadius = 0.45f;
    public LayerMask enemyLayerMask;
    public bool affectPassivePrey;
    public bool replaceExistingTotem = true;

    [Header("Cost")]
    [Min(0f)] public float staminaCost = 25f;

    [Header("Animation")]
    public bool playReleaseAnimation = true;

    public override PlayerAbilityRuntime CreateRuntime()
    {
        return new DecoyTotemRuntime();
    }

    public override float GetStaminaCost(PlayerAbilityContext context)
    {
        return ResolveStaminaCost(staminaCost, context);
    }

    public override void OnButtonDown(PlayerAbilityRuntime runtime, PlayerAbilityContext context)
    {
        PlayerStats playerStats = context.stats;
        PlayerBowController playerBow = context.bow;
        DecoyTotemRuntime decoyRuntime = runtime as DecoyTotemRuntime;

        if (decoyRuntime == null || playerStats == null || playerBow == null)
            return;

        if (!decoyRuntime.IsReady(context))
            return;

        if (!replaceExistingTotem && decoyRuntime.HasActiveTotem)
            return;

        float resolvedStaminaCost = GetStaminaCost(context);
        if (resolvedStaminaCost > 0f && !playerStats.TryConsumeStamina(resolvedStaminaCost))
            return;

        Vector2 targetPoint = ResolveTargetPoint(playerBow);
        DecoyTotemSettings settings = new DecoyTotemSettings
        {
            maxHealth = maxHealth,
            duration = duration,
            retargetRadius = retargetRadius,
            retargetInterval = retargetInterval,
            targetColliderRadius = targetColliderRadius,
            enemyLayerMask = enemyLayerMask,
            affectPassivePrey = affectPassivePrey
        };

        decoyRuntime.BeginAbilityUse(context);
        if (!decoyRuntime.PlaceTotem(totemPrefab, targetPoint, settings, replaceExistingTotem))
            return;

        if (playReleaseAnimation)
            playerBow.RequestAbilityReleaseTowards(targetPoint);

        decoyRuntime.StartCooldown(context);
        PlayerShootDebug.Log(playerBow, "DecoyTotem", $"Placed at {FormatVector(targetPoint)} duration={duration:F2} radius={retargetRadius:F2} hp={maxHealth:F0}.");
    }

    public override void Tick(PlayerAbilityRuntime runtime, PlayerAbilityContext context)
    {
        if (runtime is DecoyTotemRuntime decoyRuntime)
            decoyRuntime.Tick();
    }

    private Vector2 ResolveTargetPoint(PlayerBowController playerBow)
    {
        Vector2 playerPosition = playerBow.transform.position;
        Vector2 rawTarget = playerBow.GetPointerWorldPoint();

        if (maxPlacementRange <= 0f)
            return rawTarget;

        Vector2 offset = rawTarget - playerPosition;
        float maxRangeSqr = maxPlacementRange * maxPlacementRange;
        if (offset.sqrMagnitude <= maxRangeSqr)
            return rawTarget;

        if (offset.sqrMagnitude <= MinDirectionSqrMagnitude)
            return playerPosition;

        return playerPosition + (offset.normalized * maxPlacementRange);
    }

    private static string FormatVector(Vector2 value)
    {
        return $"({value.x:F2}, {value.y:F2})";
    }
}
