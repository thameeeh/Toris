using UnityEngine;

[CreateAssetMenu(fileName = "RambowConfig", menuName = "Game/Abilities/Rambow")]
public class RamboBowConfig : ScriptableObject
{
    [Header("Unlock Requirements")]
    [Tooltip("ResourceData stat that tracks total kills.")]
    public ResourceData killsStat;

    [Tooltip("How many kills needed to unlock Rambo mode.")]
    [Min(0)] public int killsRequired = 30;

    [Header("Firing Behaviour")]
    [Tooltip("Shots per second while holding the fire button in Rambo mode.")]
    [Min(0.1f)] public float shotsPerSecond = 8f;

    [Tooltip("Spread in degrees for the minigun stream.")]
    [Min(0f)] public float spreadDegrees = 6f;

    [Tooltip("Base damage for each Rambo arrow.")]
    [Min(0f)] public float damagePerShot = 8f;

    [Tooltip("Arrow speed in Rambo mode.")]
    [Min(0.1f)] public float speedPerShot = 12f;

    [Header("Cost")]
    [Min(0f)] public float initialStaminaCost = 10f;
    [Min(0f)] public float staminaPerShot = 2f;

    [Header("Misc")]
    [Tooltip("Optional: limit how long Rambo mode can be held (0 = unlimited).")]
    [Min(0f)] public float maxDuration = 0f;
}
