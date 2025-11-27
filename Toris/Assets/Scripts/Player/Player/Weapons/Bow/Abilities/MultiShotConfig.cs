using UnityEngine;

[CreateAssetMenu(fileName = "MultiShotConfig", menuName = "Game/Abilities/Multi Shot")]
public class MultiShotConfig : ScriptableObject
{
    [Header("Pattern")]
    [Min(1)] public int arrowCount = 3;
    [Min(0f)] public float totalSpreadDegrees = 20f;

    [Header("Cost & Cooldown")]
    [Min(0f)] public float staminaCost = 25f;
    [Min(0f)] public float cooldown = 8f;
}
