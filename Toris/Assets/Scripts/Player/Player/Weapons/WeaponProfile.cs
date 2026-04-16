using UnityEngine;

[CreateAssetMenu(fileName = "Weapon Profile", menuName = "Game/Weapons/Weapon Profile")]
public class WeaponProfile : ScriptableObject
{
    [System.Serializable]
    public class ActionDef
    {
        [Tooltip("Logical key the controller asks for (e.g., \"Shoot\", \"HeavyShoot\"")]
        public string actionKey = "ShootF";

        [Header("Animation Behavior")]
        public bool usesLock = true;
        [Range(0f, 1f)] public float lockAt = 0.48f;
        [Range(0f, 0.3f)] public float crossFade = 0.05f;
        [Min(0f)] public float repeatWindow = 0f;

        [Header("Naming")]
        [Tooltip("If set, replaces character's default suffix for this action (e.g., Bow uses 'Shoot', Crossbow uses 'CrossbowShoot')")]
        public string animSuffixOverride = ""; // empty = default
    }

    [Header("Action Set")]
    public ActionDef[] actions = new[]
    {
        new ActionDef { actionKey = "ShootF", usesLock = true, lockAt = 0.24f, crossFade = 0.05f, repeatWindow = 0f, animSuffixOverride = "" },
        new ActionDef { actionKey = "ShootS", usesLock = true, lockAt = 0.35f, crossFade = 0.05f, repeatWindow = 0.45f, animSuffixOverride = "" },
        new ActionDef { actionKey = "Dash", usesLock = false, lockAt = 0f, crossFade = 0.05f, repeatWindow = 0f, animSuffixOverride = "" }
    };

    public ActionDef Get(string key)
    {
        foreach (var a in actions) if (a.actionKey == key) return a;
        return null;
    }
}
