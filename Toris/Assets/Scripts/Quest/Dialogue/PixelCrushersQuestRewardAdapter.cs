using System;
using System.Collections.Generic;
using System.Text;
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

    private static readonly List<PixelCrushersQuestRewardAdapter> ActiveAdapters = new List<PixelCrushersQuestRewardAdapter>();

    private readonly List<PixelCrushersQuestRewardDefinition> _runtimeRewards = new List<PixelCrushersQuestRewardDefinition>();
    private readonly HashSet<string> _inventoryFullRewardWarningsShown = new HashSet<string>();
    private string[] _lastObservedQuestStates = Array.Empty<string>();

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        if (!ActiveAdapters.Contains(this))
            ActiveAdapters.Add(this);

        ResolveReferences();
        RebuildRuntimeRewards();

        for (int i = 0; i < _runtimeRewards.Count; i++)
        {
            PixelCrushersQuestRewardDefinition reward = _runtimeRewards[i];
            _lastObservedQuestStates[i] = PixelCrushersQuestBridge.GetQuestStateString(reward.QuestName);
        }
    }

    private void OnDisable()
    {
        ActiveAdapters.Remove(this);
    }

    private void Update()
    {
        if (!PixelCrushersQuestBridge.HasDialogueManager || _runtimeRewards.Count == 0)
            return;

        for (int i = 0; i < _runtimeRewards.Count; i++)
        {
            PixelCrushersQuestRewardDefinition reward = _runtimeRewards[i];
            string currentState = PixelCrushersQuestBridge.GetQuestStateString(reward.QuestName);
            bool stateChanged = currentState != _lastObservedQuestStates[i];

#if UNITY_EDITOR
            if (stateChanged && _debugRewardFlow)
                Debug.Log($"[PixelCrushersQuestRewardAdapter] Quest '{reward.QuestName}' changed '{_lastObservedQuestStates[i]}' -> '{currentState}'.", this);
#endif

            if (stateChanged)
                _lastObservedQuestStates[i] = currentState;

            if (!string.Equals(currentState, PixelCrushersQuestBridge.SuccessState, StringComparison.Ordinal))
                continue;

            if (reward.ClaimMode != PixelCrushersQuestRewardClaimMode.AutomaticOnSuccess || RewardsWereAlreadyGranted(reward))
                continue;

            if (stateChanged)
                TryGrantRewards(reward);
        }
    }

    public static bool HasUnclaimedRewards(string questName)
    {
        if (string.IsNullOrWhiteSpace(questName))
            return false;

        for (int i = 0; i < ActiveAdapters.Count; i++)
        {
            if (ActiveAdapters[i] != null && ActiveAdapters[i].HasUnclaimedRewardsInternal(questName))
                return true;
        }

        return false;
    }

    public static bool TryCollectRewards(string questName)
    {
        if (string.IsNullOrWhiteSpace(questName))
            return false;

        bool foundRewards = false;
        bool allFoundRewardsClaimed = true;

        for (int i = 0; i < ActiveAdapters.Count; i++)
        {
            PixelCrushersQuestRewardAdapter adapter = ActiveAdapters[i];
            if (adapter == null)
                continue;

            if (!adapter.TryCollectRewardsInternal(questName, out bool allAdapterRewardsClaimed))
                continue;

            foundRewards = true;
            allFoundRewardsClaimed &= allAdapterRewardsClaimed;
        }

        return foundRewards && allFoundRewardsClaimed;
    }

    public static void ResetRewardClaimState(string questName)
    {
        if (string.IsNullOrWhiteSpace(questName))
            return;

        for (int i = 0; i < ActiveAdapters.Count; i++)
            ActiveAdapters[i]?.ResetRewardClaimStateInternal(questName);
    }

    public static bool TryGetRewardPreviewText(string questName, bool includeClaimStatus, out string rewardPreviewText)
    {
        rewardPreviewText = string.Empty;

        if (string.IsNullOrWhiteSpace(questName))
            return false;

        StringBuilder builder = new StringBuilder();

        for (int i = 0; i < ActiveAdapters.Count; i++)
        {
            PixelCrushersQuestRewardAdapter adapter = ActiveAdapters[i];
            if (adapter == null)
                continue;

            adapter.AppendRewardPreviewText(questName, includeClaimStatus, builder);
        }

        rewardPreviewText = builder.ToString().TrimEnd();
        return rewardPreviewText.Length > 0;
    }

    private bool HasUnclaimedRewardsInternal(string questName)
    {
        if (!IsQuestSuccessful(questName))
            return false;

        for (int i = 0; i < _runtimeRewards.Count; i++)
        {
            PixelCrushersQuestRewardDefinition reward = _runtimeRewards[i];
            if (!IsRewardForQuest(reward, questName) || !HasConfiguredRewardPieces(reward))
                continue;

            if (!RewardsWereAlreadyGranted(reward) && !AllRewardPiecesGranted(reward))
                return true;
        }

        return false;
    }

    private bool TryCollectRewardsInternal(string questName, out bool allRewardsClaimed)
    {
        allRewardsClaimed = true;

        if (!IsQuestSuccessful(questName))
            return false;

        bool foundRewards = false;

        for (int i = 0; i < _runtimeRewards.Count; i++)
        {
            PixelCrushersQuestRewardDefinition reward = _runtimeRewards[i];
            if (!IsRewardForQuest(reward, questName) || !HasConfiguredRewardPieces(reward) || RewardsWereAlreadyGranted(reward))
                continue;

            foundRewards = true;
            allRewardsClaimed &= TryGrantRewards(reward);
        }

        return foundRewards;
    }

    private void ResetRewardClaimStateInternal(string questName)
    {
        for (int i = 0; i < _runtimeRewards.Count; i++)
        {
            PixelCrushersQuestRewardDefinition reward = _runtimeRewards[i];
            if (!IsRewardForQuest(reward, questName))
                continue;

            PixelCrushersQuestBridge.SetIntVariable(reward.ResolvedRewardGrantedVariableName, 0);
            PixelCrushersQuestBridge.SetIntVariable(reward.ResolvedGoldRewardGrantedVariableName, 0);
            PixelCrushersQuestBridge.SetIntVariable(reward.ResolvedExperienceRewardGrantedVariableName, 0);
            PixelCrushersQuestBridge.SetIntVariable(reward.ResolvedItemRewardGrantedVariableName, 0);
            _inventoryFullRewardWarningsShown.Remove(reward.ResolvedItemRewardGrantedVariableName);
        }
    }

    private void AppendRewardPreviewText(string questName, bool includeClaimStatus, StringBuilder builder)
    {
        if (builder == null)
            return;

        for (int i = 0; i < _runtimeRewards.Count; i++)
        {
            PixelCrushersQuestRewardDefinition reward = _runtimeRewards[i];
            if (!IsRewardForQuest(reward, questName) || !HasConfiguredRewardPieces(reward))
                continue;

            AppendRewardPreviewLines(reward, includeClaimStatus, builder);
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

    private bool TryGrantRewards(PixelCrushersQuestRewardDefinition reward)
    {
        if (reward == null || !reward.IsConfigured)
            return false;

        if (RewardsWereAlreadyGranted(reward))
        {
#if UNITY_EDITOR
            if (_debugRewardFlow)
                Debug.Log($"[PixelCrushersQuestRewardAdapter] Rewards for '{reward.QuestName}' were already granted.", this);
#endif
            return true;
        }

        ResolveReferences();

        TryGrantGoldReward(reward);
        TryGrantExperienceReward(reward);
        TryGrantItemReward(reward);

        if (!AllRewardPiecesGranted(reward))
            return false;

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
        return true;
    }

    private bool TryGrantGoldReward(PixelCrushersQuestRewardDefinition reward)
    {
        if (reward.GoldReward <= 0 || IsGoldRewardGranted(reward))
            return true;

        if (!CanGrantProgressionReward(reward, "gold"))
            return false;

        _playerProgressionAnchor.Instance.AddGold(reward.GoldReward);
        PixelCrushersQuestBridge.SetIntVariable(reward.ResolvedGoldRewardGrantedVariableName, 1);
        return true;
    }

    private bool TryGrantExperienceReward(PixelCrushersQuestRewardDefinition reward)
    {
        if (reward.ExperienceReward <= 0 || IsExperienceRewardGranted(reward))
            return true;

        if (!CanGrantProgressionReward(reward, "experience"))
            return false;

        _playerProgressionAnchor.Instance.AddExperience(reward.ExperienceReward);
        PixelCrushersQuestBridge.SetIntVariable(reward.ResolvedExperienceRewardGrantedVariableName, 1);
        return true;
    }

    private bool TryGrantItemReward(PixelCrushersQuestRewardDefinition reward)
    {
        if (reward.ItemReward == null || IsItemRewardGranted(reward))
            return true;

        int quantity = Mathf.Max(1, reward.ItemRewardQuantity);

        if (_gameSession == null || _gameSession.PlayerInventory == null)
        {
#if UNITY_EDITOR
            if (_debugRewardFlow)
                Debug.LogWarning($"[PixelCrushersQuestRewardAdapter] Player inventory is not available for quest '{reward.QuestName}'.", this);
#endif
            return false;
        }

        ItemInstance itemReward = new ItemInstance(reward.ItemReward);

        if (!_gameSession.PlayerInventory.CanAddItem(itemReward, quantity))
        {
            LogInventoryFullRewardWarning(reward, quantity);
            return false;
        }

        if (!_gameSession.PlayerInventory.AddItem(itemReward, quantity))
        {
#if UNITY_EDITOR
            if (_debugRewardFlow)
                Debug.LogWarning($"[PixelCrushersQuestRewardAdapter] Failed to add reward item '{reward.ItemReward.ItemName}' x{quantity} for quest '{reward.QuestName}'.", this);
#endif
            return false;
        }

        _inventoryFullRewardWarningsShown.Remove(reward.ResolvedItemRewardGrantedVariableName);
        PixelCrushersQuestBridge.SetIntVariable(reward.ResolvedItemRewardGrantedVariableName, 1);
        return true;
    }

    private bool CanGrantProgressionReward(PixelCrushersQuestRewardDefinition reward, string rewardType)
    {
        if (_playerProgressionAnchor != null && _playerProgressionAnchor.IsReady)
            return true;

#if UNITY_EDITOR
        if (_debugRewardFlow)
            Debug.LogWarning($"[PixelCrushersQuestRewardAdapter] Progression anchor is not ready to grant {rewardType} for quest '{reward.QuestName}'.", this);
#endif
        return false;
    }

    private bool AllRewardPiecesGranted(PixelCrushersQuestRewardDefinition reward)
    {
        return IsGoldRewardGranted(reward)
            && IsExperienceRewardGranted(reward)
            && IsItemRewardGranted(reward);
    }

    private void AppendRewardPreviewLines(PixelCrushersQuestRewardDefinition reward, bool includeClaimStatus, StringBuilder builder)
    {
        if (reward.GoldReward > 0)
            AppendRewardPreviewLine(builder, $"Gold: {reward.GoldReward}", includeClaimStatus, IsGoldRewardGranted(reward));

        if (reward.ExperienceReward > 0)
            AppendRewardPreviewLine(builder, $"XP: {reward.ExperienceReward}", includeClaimStatus, IsExperienceRewardGranted(reward));

        if (reward.ItemReward != null)
        {
            int quantity = Mathf.Max(1, reward.ItemRewardQuantity);
            string itemName = string.IsNullOrWhiteSpace(reward.ItemReward.ItemName)
                ? reward.ItemReward.name
                : reward.ItemReward.ItemName;
            AppendRewardPreviewLine(builder, $"{itemName} x{quantity}", includeClaimStatus, IsItemRewardGranted(reward));
        }
    }

    private static void AppendRewardPreviewLine(StringBuilder builder, string rewardText, bool includeClaimStatus, bool rewardPieceClaimed)
    {
        if (string.IsNullOrWhiteSpace(rewardText))
            return;

        builder.Append("- ");
        builder.Append(rewardText);

        if (includeClaimStatus)
            builder.Append(rewardPieceClaimed ? " (claimed)" : " (pending)");

        builder.AppendLine();
    }

    private static bool HasConfiguredRewardPieces(PixelCrushersQuestRewardDefinition reward)
    {
        return reward != null
            && (reward.GoldReward > 0 || reward.ExperienceReward > 0 || reward.ItemReward != null);
    }

    private static bool IsRewardForQuest(PixelCrushersQuestRewardDefinition reward, string questName)
    {
        return reward != null
            && string.Equals(reward.QuestName, questName, StringComparison.Ordinal);
    }

    private static bool IsQuestSuccessful(string questName)
    {
        return string.Equals(
            PixelCrushersQuestBridge.GetQuestStateString(questName),
            PixelCrushersQuestBridge.SuccessState,
            StringComparison.Ordinal);
    }

    private bool IsGoldRewardGranted(PixelCrushersQuestRewardDefinition reward)
    {
        return reward.GoldReward <= 0
            || RewardsWereAlreadyGranted(reward)
            || PixelCrushersQuestBridge.GetIntVariable(reward.ResolvedGoldRewardGrantedVariableName, 0) > 0;
    }

    private bool IsExperienceRewardGranted(PixelCrushersQuestRewardDefinition reward)
    {
        return reward.ExperienceReward <= 0
            || RewardsWereAlreadyGranted(reward)
            || PixelCrushersQuestBridge.GetIntVariable(reward.ResolvedExperienceRewardGrantedVariableName, 0) > 0;
    }

    private bool IsItemRewardGranted(PixelCrushersQuestRewardDefinition reward)
    {
        return reward.ItemReward == null
            || RewardsWereAlreadyGranted(reward)
            || PixelCrushersQuestBridge.GetIntVariable(reward.ResolvedItemRewardGrantedVariableName, 0) > 0;
    }

    private bool RewardsWereAlreadyGranted(PixelCrushersQuestRewardDefinition reward)
    {
        return PixelCrushersQuestBridge.GetIntVariable(reward.ResolvedRewardGrantedVariableName, 0) > 0;
    }

    private void LogInventoryFullRewardWarning(PixelCrushersQuestRewardDefinition reward, int quantity)
    {
#if UNITY_EDITOR
        if (!_debugRewardFlow || !_inventoryFullRewardWarningsShown.Add(reward.ResolvedItemRewardGrantedVariableName))
            return;

        Debug.LogWarning(
            $"[PixelCrushersQuestRewardAdapter] Inventory full! Reward item pending for quest '{reward.QuestName}': {reward.ItemReward.ItemName} x{quantity}. Free inventory space to claim it.",
            this);
#endif
    }

    private void ResolveReferences()
    {
        if (_gameSession == null)
            _gameSession = GameSessionSO.LoadDefault();
    }
}
