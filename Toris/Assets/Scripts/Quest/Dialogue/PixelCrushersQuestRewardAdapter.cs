using System;
using System.Collections.Generic;
using OutlandHaven.Inventory;
using OutlandHaven.UIToolkit;
using UnityEngine;

/// <summary>
/// Watches Pixel Crushers quests and grants rewards through Toris systems when they first reach success.
/// Pixel Crushers decides quest truth; this adapter only applies gameplay rewards.
/// Add this once in the scene and assign reward set assets. Do not put quest logic here.
/// </summary>
public class PixelCrushersQuestRewardAdapter : MonoBehaviour
{
    [Header("Reward Sets")]
    [Tooltip("Reward set assets watched by this adapter. Each entry grants when its Pixel Crushers quest reaches success.")]
    [SerializeField] private PixelCrushersQuestRewardSetSO[] _rewardSets = Array.Empty<PixelCrushersQuestRewardSetSO>();

    [Header("Reward Targets")]
    [Tooltip("Runtime player progression anchor used to grant gold and XP rewards.")]
    [SerializeField] private PlayerProgressionAnchorSO _playerProgressionAnchor;
    [Tooltip("Game session used to find the player inventory for item rewards. If blank, the default GameSession resource is loaded.")]
    [SerializeField] private GameSessionSO _gameSession;

#if UNITY_EDITOR
    [Header("Debug")]
    [Tooltip("Logs quest state changes and reward grant attempts. Editor only.")]
    [SerializeField] private bool _debugRewardFlow = true;
#endif

    private readonly List<PixelCrushersQuestRewardDefinition> _runtimeRewards = new List<PixelCrushersQuestRewardDefinition>();
    private string[] _lastObservedQuestStates = Array.Empty<string>();

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        RebuildRuntimeRewards();

        for (int i = 0; i < _runtimeRewards.Count; i++)
        {
            PixelCrushersQuestRewardDefinition reward = _runtimeRewards[i];
            _lastObservedQuestStates[i] = PixelCrushersQuestBridge.GetQuestStateString(reward.QuestName);

            if (string.Equals(_lastObservedQuestStates[i], PixelCrushersQuestBridge.SuccessState, StringComparison.Ordinal))
                TryGrantRewards(reward);
        }
    }

    private void Update()
    {
        if (!PixelCrushersQuestBridge.HasDialogueManager || _runtimeRewards.Count == 0)
            return;

        for (int i = 0; i < _runtimeRewards.Count; i++)
        {
            PixelCrushersQuestRewardDefinition reward = _runtimeRewards[i];
            string currentState = PixelCrushersQuestBridge.GetQuestStateString(reward.QuestName);
            if (currentState == _lastObservedQuestStates[i])
                continue;

#if UNITY_EDITOR
            if (_debugRewardFlow)
                Debug.Log($"[PixelCrushersQuestRewardAdapter] Quest '{reward.QuestName}' changed '{_lastObservedQuestStates[i]}' -> '{currentState}'.", this);
#endif

            _lastObservedQuestStates[i] = currentState;

            if (!string.Equals(currentState, PixelCrushersQuestBridge.SuccessState, StringComparison.Ordinal))
                continue;

            TryGrantRewards(reward);
        }
    }

    private void RebuildRuntimeRewards()
    {
        _runtimeRewards.Clear();

        if (_rewardSets != null)
        {
            for (int i = 0; i < _rewardSets.Length; i++)
            {
                if (_rewardSets[i] == null)
                    continue;

                _rewardSets[i].AppendRewardsTo(_runtimeRewards);
            }
        }

        _lastObservedQuestStates = new string[_runtimeRewards.Count];
    }

    private void TryGrantRewards(PixelCrushersQuestRewardDefinition reward)
    {
        if (reward == null || !reward.IsConfigured)
            return;

        if (RewardsWereAlreadyGranted(reward))
        {
#if UNITY_EDITOR
            if (_debugRewardFlow)
                Debug.Log($"[PixelCrushersQuestRewardAdapter] Rewards for '{reward.QuestName}' were already granted.", this);
#endif
            return;
        }

        ResolveReferences();

        if (!CanGrantConfiguredRewards(reward))
            return;

        if (reward.GoldReward > 0)
            _playerProgressionAnchor.Instance.AddGold(reward.GoldReward);

        if (reward.ExperienceReward > 0)
            _playerProgressionAnchor.Instance.AddExperience(reward.ExperienceReward);

        if (reward.ItemReward != null)
            _gameSession.PlayerInventory.AddItem(new ItemInstance(reward.ItemReward), reward.ItemRewardQuantity);

        PixelCrushersQuestBridge.SetIntVariable(reward.ResolvedRewardGrantedVariableName, 1);

#if UNITY_EDITOR
        if (_debugRewardFlow)
        {
            string itemSummary = reward.ItemReward != null
                ? $", item={reward.ItemReward.ItemName} x{reward.ItemRewardQuantity}"
                : string.Empty;
            Debug.Log(
                $"[PixelCrushersQuestRewardAdapter] Granted rewards for '{reward.QuestName}': gold={reward.GoldReward}, xp={reward.ExperienceReward}{itemSummary}.",
                this);
        }
#endif
    }

    private bool CanGrantConfiguredRewards(PixelCrushersQuestRewardDefinition reward)
    {
        if ((reward.GoldReward > 0 || reward.ExperienceReward > 0) && (_playerProgressionAnchor == null || !_playerProgressionAnchor.IsReady))
        {
#if UNITY_EDITOR
            if (_debugRewardFlow)
                Debug.LogWarning($"[PixelCrushersQuestRewardAdapter] Progression anchor is not ready for quest '{reward.QuestName}'.", this);
#endif
            return false;
        }

        if (reward.ItemReward == null)
            return true;

        if (_gameSession == null || _gameSession.PlayerInventory == null)
        {
#if UNITY_EDITOR
            if (_debugRewardFlow)
                Debug.LogWarning($"[PixelCrushersQuestRewardAdapter] Player inventory is not available for quest '{reward.QuestName}'.", this);
#endif
            return false;
        }

        if (!_gameSession.PlayerInventory.CanAddItem(new ItemInstance(reward.ItemReward), reward.ItemRewardQuantity))
        {
#if UNITY_EDITOR
            if (_debugRewardFlow)
                Debug.LogWarning($"[PixelCrushersQuestRewardAdapter] Not enough inventory space for reward item '{reward.ItemReward.ItemName}' x{reward.ItemRewardQuantity}.", this);
#endif
            return false;
        }

        return true;
    }

    private bool RewardsWereAlreadyGranted(PixelCrushersQuestRewardDefinition reward)
    {
        return PixelCrushersQuestBridge.GetIntVariable(reward.ResolvedRewardGrantedVariableName, 0) > 0;
    }

    private void ResolveReferences()
    {
        if (_gameSession == null)
            _gameSession = GameSessionSO.LoadDefault();
    }
}
