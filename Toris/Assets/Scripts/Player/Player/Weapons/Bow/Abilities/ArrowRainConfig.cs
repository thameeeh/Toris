using UnityEngine;

[CreateAssetMenu(fileName = "ArrowRainConfig", menuName = "Game/Abilities/Arrow Rain")]
public class ArrowRainConfig : PlayerAbilitySO
{
    [Header("Targeting")]
    [Min(0f)] public float maxTargetRange = 10f;

    [Header("Zone")]
    [Min(0.1f)] public float zoneRadius = 3f;
    [Min(0.05f)] public float rainDuration = 3f;
    [Min(0f)] public float firstBurstDelay = 0.05f;
    [Min(0.05f)] public float burstInterval = 0.35f;
    [Min(1)] public int strikesPerBurst = 6;
    public bool guaranteeCenterStrike = true;

    [Header("Strike")]
    [Min(0.05f)] public float impactRadius = 0.9f;
    [Min(0f)] public float damagePerStrike = 12f;

    [Header("Visual")]
    public GameObject arrowRainVisualPrefab;
    public bool spawnVisualArrows = true;
    [Min(0f)] public float visualArrowHeight = 4.5f;
    [Min(0.1f)] public float visualArrowSpeed = 28f;
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

        arrowRainRuntime.RefreshState();

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
        arrowRainRuntime.Activate(rainDuration);

        if (playReleaseAnimation)
        {
            playerBow.RequestAbilityReleaseTowards(targetPoint);
        }

        playerBow.StartArrowRain(
            targetPoint,
            new PlayerBowController.ArrowRainZoneSettings
            {
                castOrigin = castOrigin,
                maxTargetRange = maxTargetRange,
                duration = rainDuration,
                firstBurstDelay = firstBurstDelay,
                burstInterval = burstInterval,
                strikesPerBurst = strikesPerBurst,
                zoneRadius = zoneRadius,
                impactRadius = impactRadius,
                guaranteeCenterStrike = guaranteeCenterStrike,
                damagePerStrike = damagePerStrike,
                arrowRainVisualPrefab = arrowRainVisualPrefab,
                spawnVisualArrows = spawnVisualArrows,
                visualArrowHeight = visualArrowHeight,
                visualArrowSpeed = visualArrowSpeed,
                playImpactEffect = playImpactEffect
            });

        arrowRainRuntime.StartCooldown();
    }

    public override void Tick(PlayerAbilityRuntime runtime, PlayerAbilityContext context)
    {
        if (runtime is ArrowRainRuntime arrowRainRuntime)
        {
            arrowRainRuntime.RefreshState();
        }
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
