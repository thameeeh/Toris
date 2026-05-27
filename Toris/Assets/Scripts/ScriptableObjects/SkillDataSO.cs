using UnityEngine;

public enum SkillCategory
{
    Player,
    Weapon
}

[CreateAssetMenu(fileName = "NewSkill", menuName = "Outland Haven/Player/Skills/Skill")]
public class SkillData : ScriptableObject
{
    [Header("Core Identity")]
    public string skillID; // Crucial for saving/loading and display
    public SkillCategory category = SkillCategory.Player;

    [Header("Display Info")]
    [TextArea(3, 5)]
    public string description;
    public int costSP; // How many Skill Points it takes to unlock
    // public Texture2D icon; // Uncomment if you add icons later
    // public RenderTexture videoPreview; // Placeholder for video support

    [Header("Tree Architecture")]
    public SkillData[] prerequisites; // What skills must be unlocked first?

    [Header("Ability Integration")]
    [Tooltip("If this skill grants an active combat ability, assign it here.")]
    public PlayerAbilitySO associatedAbility;
}