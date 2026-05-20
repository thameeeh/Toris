using System.Collections;
using System.Collections.Generic;
using OutlandHaven.Inventory;
using OutlandHaven.SaveSystem;
using OutlandHaven.UIToolkit;
using UnityEngine;
using UnityEngine.SceneManagement;

// Gameplay-side death screen handler: restores the run checkpoint, applies penalties, and returns to MainArea.
public sealed class DeathRespawnCoordinator : MonoBehaviour
{
    private const string DefaultDeathInputLockId = "Death";
    private const string DefaultMainAreaSceneName = "MainArea";
    private const string DefaultMainMenuSceneName = "MainMenu";
    private const string DefaultRespawnAnchorId = "MainArea_DeathRespawn";
    private const float DefaultExperienceLossPercent = 0.1f;
    private const float DefaultGoldLossPercent = 0.1f;
    private const float DefaultBackpackItemLossPercent = 0.1f;
    private const float DefaultPotionItemLossPercent = 0.1f;
    private const float ApplyTimeoutSeconds = 3f;

    [Header("Dependencies")]
    [SerializeField] private UIEventsSO _uiEvents;
    [SerializeField] private GameSessionSO _gameSession;
    [SerializeField] private SaveManager _saveManager;
    [SerializeField] private DeathPenaltyConfigSO _penaltyConfig;

    [Header("Respawn")]
    [SerializeField] private string _mainAreaSceneName = DefaultMainAreaSceneName;
    [SerializeField] private string _mainMenuSceneName = DefaultMainMenuSceneName;
    [SerializeField] private string _respawnAnchorId = DefaultRespawnAnchorId;
    [SerializeField] private bool _saveAfterRespawnInMainArea = true;

    private static PendingDeathRespawn _pendingRespawn;
    private Coroutine _applyPendingRespawnRoutine;

    public static bool HasPendingRespawn => _pendingRespawn != null;

    private void OnEnable()
    {
        ResolveDependencies();

        if (_uiEvents != null)
        {
            _uiEvents.OnDeathRespawnRequested += HandleDeathRespawnRequested;
            _uiEvents.OnDeathMainMenuRequested += HandleDeathMainMenuRequested;
        }
    }

    private void Start()
    {
        TryStartPendingRespawnRoutine();
    }

    private void OnDisable()
    {
        if (_uiEvents != null)
        {
            _uiEvents.OnDeathRespawnRequested -= HandleDeathRespawnRequested;
            _uiEvents.OnDeathMainMenuRequested -= HandleDeathMainMenuRequested;
        }

        if (_applyPendingRespawnRoutine != null)
        {
            StopCoroutine(_applyPendingRespawnRoutine);
            _applyPendingRespawnRoutine = null;
        }
    }

    private void HandleDeathRespawnRequested()
    {
        if (_pendingRespawn != null)
            return;

        ResolveDependencies();

        _pendingRespawn = new PendingDeathRespawn(
            ResolveSceneName(_mainAreaSceneName, DefaultMainAreaSceneName),
            ResolveSceneName(_respawnAnchorId, DefaultRespawnAnchorId),
            _saveAfterRespawnInMainArea,
            _penaltyConfig);

        LoadScene(_pendingRespawn.MainAreaSceneName);
    }

    private void HandleDeathMainMenuRequested()
    {
        _pendingRespawn = null;
        Time.timeScale = 1f;
        _uiEvents?.OnGameplayInputUnlockRequested?.Invoke(DefaultDeathInputLockId);
        LoadScene(ResolveSceneName(_mainMenuSceneName, DefaultMainMenuSceneName));
    }

    private void TryStartPendingRespawnRoutine()
    {
        if (_pendingRespawn == null || _applyPendingRespawnRoutine != null)
            return;

        string currentSceneName = SceneManager.GetActiveScene().name;
        if (!SceneNameEquals(currentSceneName, _pendingRespawn.MainAreaSceneName))
            return;

        _applyPendingRespawnRoutine = StartCoroutine(ApplyPendingRespawnWhenReady());
    }

    private IEnumerator ApplyPendingRespawnWhenReady()
    {
        float deadline = Time.realtimeSinceStartup + ApplyTimeoutSeconds;
        yield return null;

        while (Time.realtimeSinceStartup < deadline && !HasMinimumRespawnTargetsReady())
        {
            yield return null;
        }

        ApplyPendingRespawn();
        _applyPendingRespawnRoutine = null;
    }

    private void ApplyPendingRespawn()
    {
        PendingDeathRespawn respawn = _pendingRespawn;
        if (respawn == null)
            return;

        ResolveDependencies();
        // Restore after MainArea loads so ProceduralTiles OnDisable snapshots cannot overwrite the death checkpoint.
        RestoreCheckpointFromActiveSaveSlot();

        PlayerStats stats = ResolvePlayerStats();
        PlayerProgression progression = ResolvePlayerProgression();
        DeathPenaltyConfigSO resolvedPenaltyConfig = respawn.PenaltyConfig != null
            ? respawn.PenaltyConfig
            : _penaltyConfig;

        MovePlayerToRespawnAnchor(stats, respawn.RespawnAnchorId);
        RestorePlayerResources(stats);
        ClearPlayerStatuses(stats);
        EnablePlayerLifeGate(stats);
        ApplyProgressionPenalty(progression, resolvedPenaltyConfig);
        ApplyInventoryPenalty(
            _gameSession != null ? _gameSession.PlayerInventory : null,
            GetBackpackLossPercent(resolvedPenaltyConfig),
            ShouldRemoveAtLeastOneItem(resolvedPenaltyConfig));
        ApplyInventoryPenalty(
            _gameSession != null ? _gameSession.PlayerPotionInventory : null,
            GetPotionLossPercent(resolvedPenaltyConfig),
            ShouldRemoveAtLeastOneItem(resolvedPenaltyConfig));
        CaptureRuntimeSnapshots(stats, progression);

        if (respawn.SaveAfterRespawnInMainArea)
        {
            SaveRespawnState();
        }

        Time.timeScale = 1f;
        _uiEvents?.OnGameplayInputUnlockRequested?.Invoke(DefaultDeathInputLockId);
        _pendingRespawn = null;
    }

    private void RestoreCheckpointFromActiveSaveSlot()
    {
        ResolveDependencies();

        if (_saveManager == null || _gameSession == null || _saveManager.MasterItemDatabase == null)
            return;

        GameSaveData checkpointData = _saveManager.LoadGameData(_gameSession.ActiveSaveSlot);
        if (checkpointData == null)
            return;

        _saveManager.MasterItemDatabase.Initialize();
        _gameSession.ImportFromSaveData(checkpointData, _saveManager.MasterItemDatabase);
    }

    private void MovePlayerToRespawnAnchor(PlayerStats stats, string anchorId)
    {
        if (stats == null || !DeathRespawnAnchor.TryFind(anchorId, out DeathRespawnAnchor anchor))
            return;

        Transform playerTransform = stats.transform;
        playerTransform.position = anchor.transform.position;

        if (stats.TryGetComponent(out Rigidbody2D rb))
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    private static void RestorePlayerResources(PlayerStats stats)
    {
        if (stats == null)
            return;

        stats.SetRuntimeState(stats.maxHP, stats.maxStamina);
    }

    private static void ClearPlayerStatuses(PlayerStats stats)
    {
        if (stats != null && stats.TryGetComponent(out PlayerStatusController statusController))
        {
            statusController.ClearAllStatuses();
        }
    }

    private static void EnablePlayerLifeGate(PlayerStats stats)
    {
        if (stats != null && stats.TryGetComponent(out PlayerLifeGate lifeGate))
        {
            lifeGate.RespawnEnableAll();
        }
    }

    private static void ApplyProgressionPenalty(PlayerProgression progression, DeathPenaltyConfigSO config)
    {
        if (progression == null)
            return;

        float experienceLoss = progression.CurrentExperience * GetExperienceLossPercent(config);
        if (experienceLoss > 0f)
        {
            progression.RemoveExperience(experienceLoss);
        }

        int goldLoss = Mathf.CeilToInt(progression.CurrentGold * GetGoldLossPercent(config));
        if (goldLoss > 0)
        {
            progression.TrySpendGold(goldLoss);
        }
    }

    private static void ApplyInventoryPenalty(InventoryManager inventory, float lossPercent, bool removeAtLeastOneItem)
    {
        if (inventory == null || inventory.LiveSlots == null || lossPercent <= 0f)
            return;

        List<InventorySlot> eligibleSlots = new List<InventorySlot>();
        int totalItemCount = 0;

        for (int i = 0; i < inventory.LiveSlots.Count; i++)
        {
            InventorySlot slot = inventory.LiveSlots[i];
            if (slot == null || slot.IsEmpty || slot.Count <= 0)
                continue;

            eligibleSlots.Add(slot);
            totalItemCount += slot.Count;
        }

        if (totalItemCount <= 0)
            return;

        int itemsToRemove = Mathf.CeilToInt(totalItemCount * Mathf.Clamp01(lossPercent));
        if (removeAtLeastOneItem && itemsToRemove <= 0)
        {
            itemsToRemove = 1;
        }

        itemsToRemove = Mathf.Clamp(itemsToRemove, 0, totalItemCount);

        while (itemsToRemove > 0 && eligibleSlots.Count > 0)
        {
            int randomIndex = Random.Range(0, eligibleSlots.Count);
            InventorySlot slot = eligibleSlots[randomIndex];

            if (slot == null || slot.IsEmpty || slot.Count <= 0)
            {
                eligibleSlots.RemoveAt(randomIndex);
                continue;
            }

            slot.DecreaseCount(1);
            itemsToRemove--;

            if (slot.IsEmpty || slot.Count <= 0)
            {
                eligibleSlots.RemoveAt(randomIndex);
            }
        }

        inventory.NotifyInventoryUpdated();
    }

    private void CaptureRuntimeSnapshots(PlayerStats stats, PlayerProgression progression)
    {
        if (_gameSession == null)
            return;

        if (stats != null)
        {
            _gameSession.CapturePlayerStatsState(stats.currentHP, stats.currentStamina);
        }

        if (progression != null)
        {
            _gameSession.CapturePlayerProgressionState(progression.CurrentLevel, progression.CurrentExperience, progression.CurrentGold);
        }

        if (_gameSession.PlayerInventory != null)
        {
            _gameSession.CapturePlayerInventoryState(_gameSession.PlayerInventory);
        }

        if (_gameSession.PlayerPotionInventory != null)
        {
            _gameSession.CapturePotionInventoryState(_gameSession.PlayerPotionInventory);
        }
    }

    private void SaveRespawnState()
    {
        ResolveDependencies();

        if (_saveManager == null || _gameSession == null)
            return;

        if (_saveManager.ActiveSession == null)
        {
            _saveManager.ActiveSession = _gameSession;
        }

        _saveManager.SaveGame(_gameSession.ActiveSaveSlot);
    }

    private PlayerStats ResolvePlayerStats()
    {
        ResolveDependencies();

        if (_gameSession != null && _gameSession.StatsAnchor != null && _gameSession.StatsAnchor.IsReady)
        {
            return _gameSession.StatsAnchor.Instance;
        }

        return FindFirstObjectByType<PlayerStats>();
    }

    private PlayerProgression ResolvePlayerProgression()
    {
        ResolveDependencies();

        if (_gameSession != null && _gameSession.ProgressionAnchor != null && _gameSession.ProgressionAnchor.IsReady)
        {
            return _gameSession.ProgressionAnchor.Instance;
        }

        return FindFirstObjectByType<PlayerProgression>();
    }

    private bool HasMinimumRespawnTargetsReady()
    {
        ResolveDependencies();

        return ResolvePlayerStats() != null
            && ResolvePlayerProgression() != null
            && _gameSession != null
            && _gameSession.PlayerInventory != null;
    }

    private void ResolveDependencies()
    {
        if (_gameSession == null)
        {
            _gameSession = GameSessionSO.LoadDefault();
        }

        if (_saveManager == null || !IsSceneInstance(_saveManager))
        {
            _saveManager = FindFirstObjectByType<SaveManager>();
        }

        if (_saveManager != null && _saveManager.ActiveSession == null && _gameSession != null)
        {
            _saveManager.ActiveSession = _gameSession;
        }
    }

    private static bool IsSceneInstance(Component component)
    {
        return component != null && component.gameObject.scene.IsValid();
    }

    private static void LoadScene(string sceneName)
    {
        if (SceneTransitionService.Instance != null)
        {
            SceneTransitionService.Instance.LoadScene(sceneName);
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    private static float GetExperienceLossPercent(DeathPenaltyConfigSO config)
    {
        return config != null ? config.ExperienceLossPercent : DefaultExperienceLossPercent;
    }

    private static float GetGoldLossPercent(DeathPenaltyConfigSO config)
    {
        return config != null ? config.GoldLossPercent : DefaultGoldLossPercent;
    }

    private static float GetBackpackLossPercent(DeathPenaltyConfigSO config)
    {
        return config != null ? config.BackpackItemLossPercent : DefaultBackpackItemLossPercent;
    }

    private static float GetPotionLossPercent(DeathPenaltyConfigSO config)
    {
        return config != null ? config.PotionItemLossPercent : DefaultPotionItemLossPercent;
    }

    private static bool ShouldRemoveAtLeastOneItem(DeathPenaltyConfigSO config)
    {
        return config == null || config.RemoveAtLeastOneItemWhenPossible;
    }

    private static string ResolveSceneName(string configuredName, string fallbackName)
    {
        return string.IsNullOrWhiteSpace(configuredName) ? fallbackName : configuredName.Trim();
    }

    private static bool SceneNameEquals(string lhs, string rhs)
    {
        return !string.IsNullOrWhiteSpace(lhs)
            && !string.IsNullOrWhiteSpace(rhs)
            && string.Equals(lhs.Trim(), rhs.Trim(), System.StringComparison.OrdinalIgnoreCase);
    }

    private sealed class PendingDeathRespawn
    {
        public PendingDeathRespawn(
            string mainAreaSceneName,
            string respawnAnchorId,
            bool saveAfterRespawnInMainArea,
            DeathPenaltyConfigSO penaltyConfig)
        {
            MainAreaSceneName = mainAreaSceneName;
            RespawnAnchorId = respawnAnchorId;
            SaveAfterRespawnInMainArea = saveAfterRespawnInMainArea;
            PenaltyConfig = penaltyConfig;
        }

        public string MainAreaSceneName { get; }
        public string RespawnAnchorId { get; }
        public bool SaveAfterRespawnInMainArea { get; }
        public DeathPenaltyConfigSO PenaltyConfig { get; }
    }
}
