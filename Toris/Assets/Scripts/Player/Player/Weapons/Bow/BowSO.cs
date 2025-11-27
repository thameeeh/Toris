using UnityEngine;

[CreateAssetMenu(fileName = "BowConfig_Starter", menuName = "Game/Weapons/Bow Config")]
public class BowSO : ScriptableObject
{
    [Header("Draw (seconds)")]
    [Min(0f)] public float minDrawTime = 0.20f;     // below this = super weak
    [Min(0f)] public float maxDrawTime = 0.90f;     // fully charged
    public AnimationCurve drawToPower = AnimationCurve.Linear(0, 0, 1, 1);

    [Header("Projectile")]
    public Projectile arrowPrefab;
    public float minArrowSpeed = 6f;
    public float maxArrowSpeed = 14f;
    public float baseDamage = 5f;
    public float maxDamage = 18f;
    public float arrowLifetime = 4f;

    [Header("Inaccuracy (worse when underdrawn/overheld)")]
    public float maxSpreadDegreesAtMin = 14f;   // terrible at underdraw
    public float maxSpreadDegreesAtMax = 4f;    // still bad at full draw
    public float overHoldExtraSpreadPerSecond = 6f; // extra spread/sec after over-hold

    [Header("Ergonomics & Penalties")]
    public float nockTime = 0.20f;          // time before arrow can fly
    public float overHoldStartsAt = 1.20f;  // after this, apply extra spread
    public float cooldownAfterShot = 0.15f; // can’t instantly re-nock

    // ---------- Helpers ----------

    // 0..1 power from draw time using your curve and min/max window
    public float EvaluatePower(float drawSeconds)
    {
        float t = Mathf.InverseLerp(minDrawTime, maxDrawTime, drawSeconds);
        t = Mathf.Clamp01(t);
        return drawToPower.Evaluate(t);
    }

    // Spread based on power + over-hold penalty (seconds spent beyond overHoldStartsAt)
    public float ComputeSpreadDegrees(float power, float overHoldExtraSec)
    {
        float baseSpread = Mathf.Lerp(maxSpreadDegreesAtMin, maxSpreadDegreesAtMax, power);
        float penalty = Mathf.Max(0f, overHoldExtraSec) * overHoldExtraSpreadPerSecond;
        return baseSpread + penalty;
    }

    public float ComputeSpeed(float power) => Mathf.Lerp(minArrowSpeed, maxArrowSpeed, power);
    public float ComputeDamage(float power) => Mathf.Lerp(baseDamage, maxDamage, power);

    [System.Serializable]
    public struct ShotStats
    {
        public float power;
        public float speed;
        public float damage;
        public float spreadDeg;
    }

    // Convenience: build all shot stats from timings
    public ShotStats BuildShotStats(float drawSeconds, float overHoldExtraSec)
    {
        float p = EvaluatePower(drawSeconds);
        return new ShotStats
        {
            power = p,
            speed = ComputeSpeed(p),
            damage = ComputeDamage(p),
            spreadDeg = ComputeSpreadDegrees(p, overHoldExtraSec)
        };
    }
}
