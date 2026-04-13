using UnityEngine;

[CreateAssetMenu(fileName = "PlayerProgressionConfig", menuName = "Game/Player/Player Progression Config")]
public class PlayerProgressionConfigSO : ScriptableObject
{
    [Header("Starting Values")]
    [Min(1)] public int startingLevel = 1;
    [Min(0f)] public float startingExperience = 0f;
    [Min(0)] public int startingGold = 0;

    [Header("Leveling")]
    [Min(1)] public int experiencePerLevel = 100;
}
