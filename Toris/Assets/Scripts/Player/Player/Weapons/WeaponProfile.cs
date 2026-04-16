using UnityEngine;

[CreateAssetMenu(fileName = "Weapon Profile", menuName = "Game/Weapons/Weapon Profile")]
public class WeaponProfile : ScriptableObject
{
    [System.Serializable]
    public class ActionDef
    {
        [Tooltip("Logical key the controller asks for (e.g., \"Shoot\", \"HeavyShoot\"")]
        public string actionKey = "Shoot";

        [Header("Animation Behavior")]
        [Range(0f, 0.3f)] public float crossFade = 0.05f;

        [Header("Naming")]
        [Tooltip("If set, replaces character's default suffix for this action (e.g., Bow uses 'Shoot', Crossbow uses 'CrossbowShoot')")]
        public string animSuffixOverride = ""; // empty = default
    }

    [Header("Action Set")]
    public ActionDef[] actions = new[]
    {
        new ActionDef { actionKey = "ShootDraw", crossFade = 0.03f, animSuffixOverride = "" },
        new ActionDef { actionKey = "ShootHold", crossFade = 0.02f, animSuffixOverride = "" },
        new ActionDef { actionKey = "ShootRelease", crossFade = 0.02f, animSuffixOverride = "" },
        new ActionDef { actionKey = "Dash", crossFade = 0.05f, animSuffixOverride = "" }
    };

    public ActionDef Get(string key)
    {
        foreach (var a in actions) if (a.actionKey == key) return a;
        return null;
    }
}
