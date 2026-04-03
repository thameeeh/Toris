using UnityEngine;

[CreateAssetMenu(fileName = "Character Animation Profile", menuName = "Game/Characters/Animation/Character Anim Profile")]
public class CharacterAnimSO : ScriptableObject
{
    [Header("Base Layer")]
    public int baseLayer = 0;

    [Header("Locomotion Suffixes")]
    public string locomotionIdleSuffix = "Idle";
    public string locomotionWalkSuffix = "Walk";

    [System.Serializable]
    public class NameMap
    {
        public string actionKey;
        public string defaultSuffix;
    }

    [Header("Action Name Mapping")]
    public NameMap[] actionMap = new[]
    {
        new NameMap{ actionKey="Shoot", defaultSuffix="Shoot"},
        new NameMap{ actionKey="Hurt", defaultSuffix="Hurt"},
        new NameMap{ actionKey="Death", defaultSuffix="Death"},
        new NameMap{ actionKey="Dash", defaultSuffix="Dash"},
    };

    [Header("Animator Tags")]
    public string shootTag = "Shoot";

    public string DefaultSuffixFor(string key)
    {
        foreach (var m in actionMap)
            if (m.actionKey == key) return m.defaultSuffix;

        return key;
    }
}