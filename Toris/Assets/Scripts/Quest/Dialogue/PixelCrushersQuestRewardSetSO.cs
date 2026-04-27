using System;
using System.Collections.Generic;
using OutlandHaven.Inventory;
using UnityEngine;

/// <summary>
/// Reusable reward table keyed by Pixel Crushers quest names.
/// Pixel Crushers owns quest state; this asset only describes Toris gameplay rewards.
/// Add entries here instead of adding reward fields to scene objects.
/// </summary>
[CreateAssetMenu(
    fileName = "PixelCrushersQuestRewardSet",
    menuName = "Quest/Pixel Crushers/Quest Reward Set")]
public class PixelCrushersQuestRewardSetSO : ScriptableObject
{
    [Tooltip("Reward entries keyed by Pixel Crushers quest name. Rewards pay once when the quest reaches success.")]
    [SerializeField] private PixelCrushersQuestRewardDefinition[] _rewards = Array.Empty<PixelCrushersQuestRewardDefinition>();

    public int RewardCount => _rewards == null ? 0 : _rewards.Length;

    public void AppendRewardsTo(List<PixelCrushersQuestRewardDefinition> target)
    {
        if (target == null || _rewards == null)
            return;

        for (int i = 0; i < _rewards.Length; i++)
        {
            if (_rewards[i] != null && _rewards[i].IsConfigured)
                target.Add(_rewards[i]);
        }
    }
}

[Serializable]
public class PixelCrushersQuestRewardDefinition
{
    [Header("Pixel Crushers Quest")]
    [Tooltip("Pixel Crushers quest name that unlocks this reward when it reaches success.")]
    public string QuestName = string.Empty;
    [Tooltip("Pixel Crushers Lua variable used to remember that this reward was already paid. Leave blank to use QuestName_RewardsGranted.")]
    public string RewardGrantedVariableName = string.Empty;

    [Header("Toris Rewards")]
    [Tooltip("Gold added to the player when this reward is granted.")]
    [Min(0)] public int GoldReward;
    [Tooltip("Experience added to the player when this reward is granted.")]
    [Min(0)] public int ExperienceReward;
    [Tooltip("Optional item blueprint added to the player inventory when this reward is granted.")]
    public InventoryItemSO ItemReward;
    [Tooltip("Quantity of the item reward to add. Only used if Item Reward is assigned.")]
    [Min(1)] public int ItemRewardQuantity = 1;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(QuestName);

    public string ResolvedRewardGrantedVariableName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(RewardGrantedVariableName))
                return RewardGrantedVariableName;

            return string.IsNullOrWhiteSpace(QuestName)
                ? string.Empty
                : $"{QuestName}_RewardsGranted";
        }
    }
}
