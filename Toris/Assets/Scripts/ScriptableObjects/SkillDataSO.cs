using UnityEngine;

[CreateAssetMenu(fileName = "NewSkill", menuName = "Game/Player/Skill")]
public class SkillData : ScriptableObject
{
    [Header("Core Identity")]
    public string skillID; // Crucial for saving/loading (e.g., "SKILL_DOUBLE_JUMP")
    public string skillName;

    [Header("Display Info")]
    [TextArea(3, 5)]
    public string description;
    public int costSP; // How many Skill Points it takes to unlock
    // public Texture2D icon; // Uncomment if you add icons later

    [Header("Tree Architecture")]
    public SkillData[] prerequisites; // What skills must be unlocked first?
}