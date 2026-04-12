using System.Collections.Generic;
using UnityEngine;
using System;


[Serializable] // Makes it visible in the Unity Inspector for debugging!
public class PlayerSkillTracker
{
    [SerializeField] private int _availableSP = 0;

    // HashSet is vastly more performant for looking up "Does player have X?" than a List
    [SerializeField] private List<string> _unlockedSkillIDs = new List<string>();

    public int AvailableSP => _availableSP;

    public void AddSP(int amount)
    {
        _availableSP += amount;
    }

    public bool TryUnlockSkill(SkillData skill)
    {
        if (HasSkill(skill.skillID))
        {
            Debug.LogWarning($"Skill {skill.skillName} is already unlocked!");
            return false;
        }

        // NEW: Enforce the prerequisite check
        if (!ArePrerequisitesMet(skill))
        {
            Debug.LogWarning($"Prerequisites for {skill.skillName} are not met!");
            return false;
        }

        if (_availableSP >= skill.costSP)
        {
            _availableSP -= skill.costSP;
            _unlockedSkillIDs.Add(skill.skillID);
            return true;
        }

        Debug.LogWarning("Not enough SP!");
        return false;
    }

    public bool ArePrerequisitesMet(SkillData skill)
    {
        // If the array is empty or null, it's a base skill and always available
        if (skill.prerequisites == null || skill.prerequisites.Length == 0)
        {
            return true;
        }

        // Check if the player has EVERY skill in the prerequisites array
        foreach (var preReq in skill.prerequisites)
        {
            if (!HasSkill(preReq.skillID))
            {
                return false;
            }
        }

        return true;
    }

    public bool HasSkill(string skillID)
    {
        return _unlockedSkillIDs.Contains(skillID);
    }
}
