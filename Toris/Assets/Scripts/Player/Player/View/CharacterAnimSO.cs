using UnityEngine;

[CreateAssetMenu(fileName = "Character Animation Profile", menuName = "Game/Characters/Animation/Character Anim Profile")]
public class CharacterAnimSO : ScriptableObject
{
    [Header("Base Layer")]
    public int baseLayer = 0;

    [Header("Naming")]
    public string characterPrefix = "BowGuy";

    [Header("Locomotion Suffixes")]
    public string locomotionIdleSuffix = "Idle";
    public string locomotionWalkSuffix = "Run";

    [System.Serializable]
    public class NameMap
    {
        public string actionKey;
        public string defaultSuffix;
    }

    [Header("Action Name Mapping")]
    public NameMap[] actionMap = new[]
    {
        new NameMap{ actionKey="ShootDraw", defaultSuffix="ShootDraw"},
        new NameMap{ actionKey="ShootHold", defaultSuffix="ShootHold"},
        new NameMap{ actionKey="ShootRelease", defaultSuffix="ShootRelease"},
        new NameMap{ actionKey="Hurt", defaultSuffix="Hurt"},
        new NameMap{ actionKey="Death", defaultSuffix="Death"},
        new NameMap{ actionKey="Dash", defaultSuffix="Dash"},
        new NameMap{ actionKey="DashP", defaultSuffix="DashP"},
    };

    [Header("Animator Tags")]
    public string shootTag = "Shoot";

    public string DefaultSuffixFor(string key)
    {
        foreach (var m in actionMap)
            if (m.actionKey == key) return m.defaultSuffix;

        return key;
    }

    public string BuildStateName(string actionKey, string dirToken, string suffixOverride = "")
    {
        string suffix = string.IsNullOrEmpty(suffixOverride)
            ? DefaultSuffixFor(actionKey)
            : suffixOverride;

        return $"{characterPrefix}{suffix}_{dirToken}";
    }
}
