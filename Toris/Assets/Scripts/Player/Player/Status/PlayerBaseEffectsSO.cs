using UnityEngine;

[CreateAssetMenu(fileName = "PlayerBaseEffects", menuName = "Game/Player/Player Base Effects")]
public class PlayerBaseEffectsSO : ScriptableObject
{
    [Header("Resources")]
    [Min(1f)] public float maxHealth = 100f;
    [Min(0f)] public float healthRegenPerSecond = 0f;
    [Min(0f)] public float maxStamina = 100f;
    [Min(0f)] public float staminaRegenPerSecond = 10f;

    [Header("Movement")]
    [Min(0f)] public float moveSpeedMultiplier = 1f;
    [Min(0f)] public float dashSpeedMultiplier = 1f;

    [Header("Combat")]
    [Min(0f)] public float outgoingDamageMultiplier = 1f;
    [Min(0f)] public float incomingDamageMultiplier = 1f;

    [Header("Immunities")]
    public bool isPoisonImmune = false;
    public bool isBurningImmune = false;
    public bool isBleedingImmune = false;
}
