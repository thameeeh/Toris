using UnityEngine;

[System.Serializable]
public class EnemyLoadout
{
    [Tooltip("Optional descriptive name used for debugging or UI.")]
    public string LoadoutId = "default";

    [Tooltip("Freeform payload for abilities/gear identifiers.")]
    public string[] EquippedAbilities = new string[0];

    [Tooltip("Arbitrary stat modifier applied to the enemy when spawned.")]
    public float PowerMultiplier = 1f;

    public virtual void Apply(Enemy enemy)
    {
        if (enemy == null) return;

        enemy.MaxHealth *= Mathf.Max(0.1f, PowerMultiplier);
    }
}