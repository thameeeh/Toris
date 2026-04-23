using OutlandHaven.Inventory;
using OutlandHaven.UIToolkit;
using UnityEngine;

/// <summary>
/// Watches one Pixel Crushers quest and grants rewards through Toris systems when it first reaches success.
/// Pixel Crushers decides quest truth; this adapter only applies gameplay rewards.
/// </summary>
public class PixelCrushersQuestRewardAdapter : MonoBehaviour
{
    [Header("Quest")]
    [SerializeField] private string _questName = "Kill_3_Leader_Wolves";
    [SerializeField] private string _rewardGrantedVariableName = "Kill_3_Leader_Wolves_RewardsGranted";

    [Header("Reward Targets")]
    [SerializeField] private PlayerProgressionAnchorSO _playerProgressionAnchor;
    [SerializeField] private GameSessionSO _gameSession;

    [Header("Rewards")]
    [SerializeField, Min(0)] private int _goldReward = 0;
    [SerializeField, Min(0)] private int _experienceReward = 0;
    [SerializeField] private InventoryItemSO _itemReward;
    [SerializeField, Min(1)] private int _itemRewardQuantity = 1;

#if UNITY_EDITOR
    [Header("Debug")]
    [SerializeField] private bool _debugRewardFlow = true;
#endif

    private string _lastObservedQuestState = string.Empty;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        _lastObservedQuestState = PixelCrushersQuestBridge.GetQuestStateString(_questName);

        if (string.Equals(_lastObservedQuestState, PixelCrushersQuestBridge.SuccessState, System.StringComparison.Ordinal))
            TryGrantRewards();
    }

    private void Update()
    {
        if (!PixelCrushersQuestBridge.HasDialogueManager || string.IsNullOrWhiteSpace(_questName))
            return;

        string currentState = PixelCrushersQuestBridge.GetQuestStateString(_questName);
        if (currentState == _lastObservedQuestState)
            return;

#if UNITY_EDITOR
        if (_debugRewardFlow)
            Debug.Log($"[PixelCrushersQuestRewardAdapter] Quest '{_questName}' changed '{_lastObservedQuestState}' -> '{currentState}'.", this);
#endif

        _lastObservedQuestState = currentState;

        if (!string.Equals(currentState, PixelCrushersQuestBridge.SuccessState, System.StringComparison.Ordinal))
            return;

        TryGrantRewards();
    }

    private void TryGrantRewards()
    {
        if (RewardsWereAlreadyGranted())
        {
#if UNITY_EDITOR
            if (_debugRewardFlow)
                Debug.Log($"[PixelCrushersQuestRewardAdapter] Rewards for '{_questName}' were already granted.", this);
#endif
            return;
        }

        ResolveReferences();

        if (!CanGrantConfiguredRewards())
            return;

        if (_goldReward > 0)
            _playerProgressionAnchor.Instance.AddGold(_goldReward);

        if (_experienceReward > 0)
            _playerProgressionAnchor.Instance.AddExperience(_experienceReward);

        if (_itemReward != null)
            _gameSession.PlayerInventory.AddItem(new ItemInstance(_itemReward), _itemRewardQuantity);

        PixelCrushersQuestBridge.SetIntVariable(_rewardGrantedVariableName, 1);

#if UNITY_EDITOR
        if (_debugRewardFlow)
        {
            string itemSummary = _itemReward != null
                ? $", item={_itemReward.ItemName} x{_itemRewardQuantity}"
                : string.Empty;
            Debug.Log(
                $"[PixelCrushersQuestRewardAdapter] Granted rewards for '{_questName}': gold={_goldReward}, xp={_experienceReward}{itemSummary}.",
                this);
        }
#endif
    }

    private bool CanGrantConfiguredRewards()
    {
        if ((_goldReward > 0 || _experienceReward > 0) && (_playerProgressionAnchor == null || !_playerProgressionAnchor.IsReady))
        {
#if UNITY_EDITOR
            if (_debugRewardFlow)
                Debug.LogWarning($"[PixelCrushersQuestRewardAdapter] Progression anchor is not ready for quest '{_questName}'.", this);
#endif
            return false;
        }

        if (_itemReward == null)
            return true;

        if (_gameSession == null || _gameSession.PlayerInventory == null)
        {
#if UNITY_EDITOR
            if (_debugRewardFlow)
                Debug.LogWarning($"[PixelCrushersQuestRewardAdapter] Player inventory is not available for quest '{_questName}'.", this);
#endif
            return false;
        }

        if (!_gameSession.PlayerInventory.CanAddItem(new ItemInstance(_itemReward), _itemRewardQuantity))
        {
#if UNITY_EDITOR
            if (_debugRewardFlow)
                Debug.LogWarning($"[PixelCrushersQuestRewardAdapter] Not enough inventory space for reward item '{_itemReward.ItemName}' x{_itemRewardQuantity}.", this);
#endif
            return false;
        }

        return true;
    }

    private bool RewardsWereAlreadyGranted()
    {
        return PixelCrushersQuestBridge.GetIntVariable(_rewardGrantedVariableName, 0) > 0;
    }

    private void ResolveReferences()
    {
        if (_gameSession == null)
            _gameSession = GameSessionSO.LoadDefault();
    }
}
