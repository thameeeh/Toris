using UnityEngine;

[CreateAssetMenu(fileName = "ArrowRainConfig", menuName = "Game/Abilities/Arrow Rain")]
public class ArrowRainConfig : PlayerAbilitySO
{
    [Header("Targeting")]
    [Min(0f)] public float maxTargetRange = 10f;

    [Header("Zone")]
    [Min(0.1f)] public float zoneRadius = 3f;
    [Min(0.05f)] public float rainDuration = 3f;
    [Min(0f)] public float initialStrikeDelay = 0.05f;
    [Min(0.01f)] public float strikeInterval = 0.15f;
    public bool guaranteeCenterStrike = true;

    [Header("Strike")]
    [Min(0.05f)] public float impactRadius = 0.9f;
    [Min(0f)] public float damagePerStrike = 12f;

    [Header("Visual")]
    public GameObject arrowRainVisualPrefab;
    public bool spawnVisualArrows = true;
    [Min(0f)] public float visualArrowHeight = 4.5f;
    [Min(0.1f)] public float visualArrowSpeed = 28f;
    public GameObject impactShadowPrefab;
    public bool spawnImpactShadow = true;
    [Min(0f)] public float impactShadowStartScaleMultiplier = 0.75f;
    [Min(0f)] public float impactShadowEndScaleMultiplier = 1f;
    [Range(0f, 1f)] public float impactShadowStartAlpha = 0.2f;
    [Range(0f, 1f)] public float impactShadowEndAlpha = 0.65f;
    public bool playImpactEffect = true;

    [Header("Cost")]
    [Min(0f)] public float staminaCost = 35f;

    [Header("Animation")]
    public bool playReleaseAnimation = true;

    public override PlayerAbilityRuntime CreateRuntime()
    {
        return new ArrowRainRuntime();
    }

    public override void OnButtonDown(PlayerAbilityRuntime runtime, PlayerAbilityContext context)
    {
        PlayerStats playerStats = context.stats;
        PlayerBowController playerBow = context.bow;
        ArrowRainRuntime arrowRainRuntime = runtime as ArrowRainRuntime;

        if (arrowRainRuntime == null || playerStats == null || playerBow == null)
            return;

        if (arrowRainRuntime.IsActive)
            return;

        if (!arrowRainRuntime.IsReady(context))
            return;

        if (staminaCost > 0f && !playerStats.TryConsumeStamina(staminaCost))
            return;

        Vector2 castOrigin = playerBow.transform.position;
        Vector2 rawTargetPoint = playerBow.GetPointerWorldPoint();
        Vector2 targetPoint = ResolveTargetPoint(playerBow);
        LogArrowRain(
            playerBow,
            $"OnButtonDown. castOrigin={FormatVector(castOrigin)} rawTarget={FormatVector(rawTargetPoint)} resolvedTarget={FormatVector(targetPoint)} maxTargetRange={maxTargetRange:F2} zoneRadius={zoneRadius:F2} resolvedDistance={Vector2.Distance(castOrigin, targetPoint):F2}");

        arrowRainRuntime.BeginAbilityUse(context);
        arrowRainRuntime.Activate(
            context,
            targetPoint,
            new ArrowRainCastSettings
            {
                castOrigin = castOrigin,
                maxTargetRange = maxTargetRange,
                duration = rainDuration,
                initialStrikeDelay = initialStrikeDelay,
                strikeInterval = strikeInterval,
                zoneRadius = zoneRadius,
                impactRadius = impactRadius,
                guaranteeCenterStrike = guaranteeCenterStrike,
                damagePerStrike = damagePerStrike,
                arrowRainVisualPrefab = arrowRainVisualPrefab,
                spawnVisualArrows = spawnVisualArrows,
                visualArrowHeight = visualArrowHeight,
                visualArrowSpeed = visualArrowSpeed,
                impactShadowPrefab = impactShadowPrefab,
                spawnImpactShadow = spawnImpactShadow,
                impactShadowStartScaleMultiplier = impactShadowStartScaleMultiplier,
                impactShadowEndScaleMultiplier = impactShadowEndScaleMultiplier,
                impactShadowStartAlpha = impactShadowStartAlpha,
                impactShadowEndAlpha = impactShadowEndAlpha,
                playImpactEffect = playImpactEffect
            });

        if (playReleaseAnimation)
        {
            playerBow.RequestAbilityReleaseTowards(targetPoint);
        }

        arrowRainRuntime.StartCooldown();
    }

    public override void Tick(PlayerAbilityRuntime runtime, PlayerAbilityContext context)
    {
        if (runtime is ArrowRainRuntime arrowRainRuntime)
            arrowRainRuntime.Tick(context);
    }

    private Vector2 ResolveTargetPoint(PlayerBowController playerBow)
    {
        Vector2 playerPosition = playerBow.transform.position;
        Vector2 rawTarget = playerBow.GetPointerWorldPoint();

        if (maxTargetRange <= 0f)
            return rawTarget;

        Vector2 offset = rawTarget - playerPosition;
        float allowedCenterRange = Mathf.Max(0f, maxTargetRange - Mathf.Max(0f, zoneRadius));
        float maxRangeSqr = allowedCenterRange * allowedCenterRange;
        if (offset.sqrMagnitude <= maxRangeSqr)
            return rawTarget;

        if (offset.sqrMagnitude <= 0.0001f)
            return playerPosition;

        return playerPosition + (offset.normalized * allowedCenterRange);
    }

    private static void LogArrowRain(Object context, string message)
    {
        PlayerShootDebug.Log(context, "ArrowRain", message);
    }

    private static string FormatVector(Vector2 value)
    {
        return $"({value.x:F2}, {value.y:F2})";
    }
}
