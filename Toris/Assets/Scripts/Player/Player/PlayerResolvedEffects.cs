using UnityEngine;

[System.Serializable]
public struct PlayerResolvedEffects
{
    public float maxHealth;
    public float maxStamina;
    public float staminaRegenPerSecond;

    public float moveSpeedMultiplier;
    public float dashSpeedMultiplier;
    public float dashDistanceMultiplier;

    public float outgoingDamageMultiplier;
    public float incomingDamageMultiplier;

    public bool isPoisonImmune;
    public bool isBurningImmune;
    public bool isBleedingImmune;

    public static PlayerResolvedEffects CreateDefault()
    {
        return new PlayerResolvedEffects
        {
            maxHealth = 100f,
            maxStamina = 100f,
            staminaRegenPerSecond = 10f,

            moveSpeedMultiplier = 1f,
            dashSpeedMultiplier = 1f,
            dashDistanceMultiplier = 1f,

            outgoingDamageMultiplier = 1f,
            incomingDamageMultiplier = 1f,

            isPoisonImmune = false,
            isBurningImmune = false,
            isBleedingImmune = false
        };
    }
}