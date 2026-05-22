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
    private const string RespawningLoadingMessage = "Respawning";
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
    private static DeathPenaltyPlan _currentPenaltyPlan;
    private Coroutine _applyPendingRespawnRoutine;

    public static bool HasPendingRespawn => _pendingRespawn != null;

    private void OnEnable()
    {
        ResolveDependencies();

        if (_uiEvents != null)
        {
            _uiEvents.OnDeathPenaltySummaryRequested += HandleDeathPenaltySummaryRequested;
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
            _uiEvents.OnDeathPenaltySummaryRequested -= HandleDeathPenaltySummaryRequested;
            _uiEvents.OnDeathRespawnRequested -= HandleDeathRespawnRequested;
            _uiEvents.OnDeathMainMenuRequested -= HandleDeathMainMenuRequested;
        }

        if (_applyPendingRespawnRoutine != null)
        {
            StopCoroutine(_applyPendingRespawnRoutine);
            _applyPendingRespawnRoutine = null;
        }
    }

    private void HandleDeathPenaltySummaryRequested()
    {
        DeathPenaltyPlan penaltyPlan = GetOrCreateCurrentPenaltyPlan();
        _uiEvents?.OnDeathPenaltySummaryUpdated?.Invoke(penaltyPlan?.Summary);
    }

    private void HandleDeathRespawnRequested()
    {
        if (_pendingRespawn != null)
            return;

        ResolveDependencies();
        DeathPenaltyPlan penaltyPlan = GetOrCreateCurrentPenaltyPlan();

        _pendingRespawn = new PendingDeathRespawn(
            ResolveSceneName(_mainAreaSceneName, DefaultMainAreaSceneName),
            ResolveSceneName(_respawnAnchorId, DefaultRespawnAnchorId),
            _saveAfterRespawnInMainArea,
            _penaltyConfig,
            penaltyPlan);
        _currentPenaltyPlan = null;

        LoadScene(_pendingRespawn.MainAreaSceneName, RespawningLoadingMessage);
    }

    private void HandleDeathMainMenuRequested()
    {
        ResolveDependencies();
        DeathPenaltyPlan penaltyPlan = GetOrCreateCurrentPenaltyPlan();
        ApplyPenaltyToActiveSaveData(penaltyPlan);

        _pendingRespawn = null;
        _currentPenaltyPlan = null;
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
        DeathPenaltyPlan penaltyPlan = respawn.PenaltyPlan ?? CreatePenaltyPlan(resolvedPenaltyConfig);

        MovePlayerToRespawnAnchor(stats, respawn.RespawnAnchorId);
        RestorePlayerResources(stats);
        ClearPlayerStatuses(stats);
        EnablePlayerLifeGate(stats);
        ApplyProgressionPenalty(progression, penaltyPlan);
        ApplyInventoryPenaltyPlan(
            _gameSession != null ? _gameSession.PlayerInventory : null,
            penaltyPlan?.BackpackLosses);
        ApplyInventoryPenaltyPlan(
            _gameSession != null ? _gameSession.PlayerPotionInventory : null,
            penaltyPlan?.PotionLosses);
        CaptureRuntimeSnapshots(stats, progression);

        if (respawn.SaveAfterRespawnInMainArea)
        {
            SaveRespawnState();
        }

        Time.timeScale = 1f;
        _uiEvents?.OnGameplayInputUnlockRequested?.Invoke(DefaultDeathInputLockId);
        _pendingRespawn = null;
        _currentPenaltyPlan = null;
    }

    private void ApplyPenaltyToActiveSaveData(DeathPenaltyPlan penaltyPlan)
    {
        // Death screen related: Main Menu also consumes death penalties by writing
        // the penalized checkpoint directly, without exporting ProceduralTiles state.
        if (_saveManager == null || _gameSession == null || penaltyPlan == null)
            return;

        GameSaveData checkpointData = LoadActiveCheckpointData();
        if (checkpointData == null)
            return;

        ApplyProgressionPenaltyToSaveData(checkpointData, penaltyPlan);
        ApplySavedInventoryPenaltyPlan(checkpointData.PlayerBackpack, penaltyPlan.BackpackLosses);
        ApplySavedInventoryPenaltyPlan(checkpointData.PlayerPotion, penaltyPlan.PotionLosses);
        ApplyRespawnResourceStateToSaveData(checkpointData);
        checkpointData.SaveTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm");

        _saveManager.SaveGameData(_gameSession.ActiveSaveSlot, checkpointData);

        if (_saveManager.MasterItemDatabase != null)
        {
            _saveManager.MasterItemDatabase.Initialize();
            _gameSession.ImportFromSaveData(checkpointData, _saveManager.MasterItemDatabase);
        }
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

    private DeathPenaltyPlan GetOrCreateCurrentPenaltyPlan()
    {
        // Death screen related: cache the preview so the UI and actual respawn use identical losses.
        if (_currentPenaltyPlan != null)
            return _currentPenaltyPlan;

        ResolveDependencies();
        _currentPenaltyPlan = CreatePenaltyPlan(_penaltyConfig);
        return _currentPenaltyPlan;
    }

    private DeathPenaltyPlan CreatePenaltyPlan(DeathPenaltyConfigSO config)
    {
        ResolveDependencies();

        GameSaveData checkpointData = LoadActiveCheckpointData();
        if (checkpointData != null)
        {
            return CreatePenaltyPlanFromSaveData(checkpointData, config);
        }

        return CreatePenaltyPlanFromRuntime(config);
    }

    private GameSaveData LoadActiveCheckpointData()
    {
        if (_saveManager == null || _gameSession == null)
            return null;

        return _saveManager.LoadGameData(_gameSession.ActiveSaveSlot);
    }

    private DeathPenaltyPlan CreatePenaltyPlanFromSaveData(GameSaveData checkpointData, DeathPenaltyConfigSO config)
    {
        List<InventoryLossEntry> backpackLosses = BuildSavedInventoryLossPlan(
            checkpointData.PlayerBackpack,
            GetBackpackLossPercent(config),
            ShouldRemoveAtLeastOneItem(config),
            DeathPenaltyInventorySource.Backpack);

        List<InventoryLossEntry> potionLosses = BuildSavedInventoryLossPlan(
            checkpointData.PlayerPotion,
            GetPotionLossPercent(config),
            ShouldRemoveAtLeastOneItem(config),
            DeathPenaltyInventorySource.Potion);

        return CreatePenaltyPlan(
            checkpointData.Experience,
            checkpointData.Gold,
            backpackLosses,
            potionLosses,
            ResolveDeathCause(),
            config);
    }

    private DeathPenaltyPlan CreatePenaltyPlanFromRuntime(DeathPenaltyConfigSO config)
    {
        PlayerProgression progression = ResolvePlayerProgression();
        float currentExperience = progression != null ? progression.CurrentExperience : 0f;
        int currentGold = progression != null ? progression.CurrentGold : 0;

        List<InventoryLossEntry> backpackLosses = BuildLiveInventoryLossPlan(
            _gameSession != null ? _gameSession.PlayerInventory : null,
            GetBackpackLossPercent(config),
            ShouldRemoveAtLeastOneItem(config),
            DeathPenaltyInventorySource.Backpack);

        List<InventoryLossEntry> potionLosses = BuildLiveInventoryLossPlan(
            _gameSession != null ? _gameSession.PlayerPotionInventory : null,
            GetPotionLossPercent(config),
            ShouldRemoveAtLeastOneItem(config),
            DeathPenaltyInventorySource.Potion);

        return CreatePenaltyPlan(
            currentExperience,
            currentGold,
            backpackLosses,
            potionLosses,
            ResolveDeathCause(),
            config);
    }

    private static DeathPenaltyPlan CreatePenaltyPlan(
        float currentExperience,
        int currentGold,
        List<InventoryLossEntry> backpackLosses,
        List<InventoryLossEntry> potionLosses,
        DeathCauseSnapshot causeOfDeath,
        DeathPenaltyConfigSO config)
    {
        float experienceLoss = Mathf.Max(0f, currentExperience * GetExperienceLossPercent(config));
        int goldLoss = Mathf.CeilToInt(Mathf.Max(0, currentGold) * GetGoldLossPercent(config));
        int backpackItemLoss = CountLossItems(backpackLosses);
        int potionItemLoss = CountLossItems(potionLosses);

        List<DeathItemLossSummary> itemSummaries = new List<DeathItemLossSummary>();
        AddLossSummaries(itemSummaries, backpackLosses);
        AddLossSummaries(itemSummaries, potionLosses);

        DeathPenaltySummary summary = new DeathPenaltySummary(
            experienceLoss,
            goldLoss,
            backpackItemLoss,
            potionItemLoss,
            causeOfDeath.DisplayName,
            itemSummaries,
            DeathCauseMessageFormatter.FormatSubtitle(causeOfDeath));

        return new DeathPenaltyPlan(summary, backpackLosses, potionLosses);
    }

    private DeathCauseSnapshot ResolveDeathCause()
    {
        PlayerStats stats = ResolvePlayerStats();
        return stats != null
            ? stats.LastDeathCause
            : DeathCauseSnapshot.Unknown();
    }

    private List<InventoryLossEntry> BuildSavedInventoryLossPlan(
        SavedInventoryData inventoryData,
        float lossPercent,
        bool removeAtLeastOneItem,
        DeathPenaltyInventorySource source)
    {
        if (inventoryData == null || inventoryData.Slots == null)
            return new List<InventoryLossEntry>();

        List<InventoryLossCandidate> candidates = new List<InventoryLossCandidate>();
        int totalItemCount = 0;

        for (int i = 0; i < inventoryData.Slots.Count; i++)
        {
            SavedSlotData slot = inventoryData.Slots[i];
            if (slot == null || slot.ItemData == null || slot.Count <= 0 || string.IsNullOrWhiteSpace(slot.ItemData.BaseItemID))
                continue;

            InventoryItemSO itemBlueprint = ResolveItemBlueprint(slot.ItemData.BaseItemID);
            string displayName = ResolveItemDisplayName(slot.ItemData.BaseItemID, itemBlueprint);
            candidates.Add(new InventoryLossCandidate(slot.ItemData.BaseItemID, displayName, slot.Count, source, itemBlueprint));
            totalItemCount += slot.Count;
        }

        return BuildInventoryLossPlan(candidates, totalItemCount, lossPercent, removeAtLeastOneItem);
    }

    private static List<InventoryLossEntry> BuildLiveInventoryLossPlan(
        InventoryManager inventory,
        float lossPercent,
        bool removeAtLeastOneItem,
        DeathPenaltyInventorySource source)
    {
        if (inventory == null || inventory.LiveSlots == null)
            return new List<InventoryLossEntry>();

        List<InventoryLossCandidate> candidates = new List<InventoryLossCandidate>();
        int totalItemCount = 0;

        for (int i = 0; i < inventory.LiveSlots.Count; i++)
        {
            InventorySlot slot = inventory.LiveSlots[i];
            if (slot == null || slot.IsEmpty || slot.Count <= 0 || slot.HeldItem?.BaseItem == null)
                continue;

            InventoryItemSO item = slot.HeldItem.BaseItem;
            string displayName = string.IsNullOrWhiteSpace(item.ItemName) ? HumanizeItemId(item.name) : item.ItemName;
            candidates.Add(new InventoryLossCandidate(item.name, displayName, slot.Count, source, item));
            totalItemCount += slot.Count;
        }

        return BuildInventoryLossPlan(candidates, totalItemCount, lossPercent, removeAtLeastOneItem);
    }

    private static List<InventoryLossEntry> BuildInventoryLossPlan(
        List<InventoryLossCandidate> candidates,
        int totalItemCount,
        float lossPercent,
        bool removeAtLeastOneItem)
    {
        List<InventoryLossEntry> losses = new List<InventoryLossEntry>();
        if (candidates == null || candidates.Count == 0 || totalItemCount <= 0 || lossPercent <= 0f)
            return losses;

        int itemsToRemove = Mathf.CeilToInt(totalItemCount * Mathf.Clamp01(lossPercent));
        if (removeAtLeastOneItem && itemsToRemove <= 0)
        {
            itemsToRemove = 1;
        }

        itemsToRemove = Mathf.Clamp(itemsToRemove, 0, totalItemCount);

        while (itemsToRemove > 0 && candidates.Count > 0)
        {
            int randomIndex = Random.Range(0, candidates.Count);
            InventoryLossCandidate candidate = candidates[randomIndex];

            if (candidate == null || candidate.Count <= 0)
            {
                candidates.RemoveAt(randomIndex);
                continue;
            }

            AddOrIncrementLoss(losses, candidate.BaseItemId, candidate.DisplayName, candidate.Source, candidate.ItemBlueprint);
            candidate.Count--;
            itemsToRemove--;

            if (candidate.Count <= 0)
            {
                candidates.RemoveAt(randomIndex);
            }
        }

        return losses;
    }

    private InventoryItemSO ResolveItemBlueprint(string baseItemId)
    {
        if (string.IsNullOrWhiteSpace(baseItemId))
            return null;

        ItemDatabaseSO itemDatabase = _saveManager != null ? _saveManager.MasterItemDatabase : null;
        return itemDatabase != null ? itemDatabase.GetItemByID(baseItemId) : null;
    }

    private static string ResolveItemDisplayName(string baseItemId, InventoryItemSO item)
    {
        if (item != null && !string.IsNullOrWhiteSpace(item.ItemName))
        {
            return item.ItemName;
        }

        return HumanizeItemId(baseItemId);
    }

    private static string HumanizeItemId(string baseItemId)
    {
        if (string.IsNullOrWhiteSpace(baseItemId))
            return "Unknown Item";

        string normalized = baseItemId.Replace('_', ' ').Replace('-', ' ').Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            return "Unknown Item";

        char[] characters = normalized.ToCharArray();
        bool capitalizeNext = true;

        for (int i = 0; i < characters.Length; i++)
        {
            if (char.IsWhiteSpace(characters[i]))
            {
                capitalizeNext = true;
                continue;
            }

            if (capitalizeNext)
            {
                characters[i] = char.ToUpperInvariant(characters[i]);
                capitalizeNext = false;
            }
        }

        return new string(characters);
    }

    private static void ApplyProgressionPenalty(PlayerProgression progression, DeathPenaltyPlan penaltyPlan)
    {
        if (progression == null || penaltyPlan == null || penaltyPlan.Summary == null)
            return;

        float experienceLoss = penaltyPlan.Summary.ExperienceLost;
        if (experienceLoss > 0f)
        {
            progression.RemoveExperience(experienceLoss);
        }

        int goldLoss = penaltyPlan.Summary.GoldLost;
        if (goldLoss > 0)
        {
            progression.TrySpendGold(goldLoss);
        }
    }

    private static void ApplyProgressionPenaltyToSaveData(GameSaveData saveData, DeathPenaltyPlan penaltyPlan)
    {
        if (saveData == null || penaltyPlan == null || penaltyPlan.Summary == null)
            return;

        saveData.Experience = Mathf.Max(0f, saveData.Experience - penaltyPlan.Summary.ExperienceLost);
        saveData.Gold = Mathf.Max(0, saveData.Gold - penaltyPlan.Summary.GoldLost);
    }

    private static void ApplyInventoryPenaltyPlan(InventoryManager inventory, IReadOnlyList<InventoryLossEntry> losses)
    {
        if (inventory == null || inventory.LiveSlots == null || losses == null || losses.Count == 0)
            return;

        bool changed = false;

        for (int lossIndex = 0; lossIndex < losses.Count; lossIndex++)
        {
            InventoryLossEntry loss = losses[lossIndex];
            if (loss == null || loss.Count <= 0 || string.IsNullOrWhiteSpace(loss.BaseItemId))
                continue;

            int remainingToRemove = loss.Count;
            for (int slotIndex = 0; slotIndex < inventory.LiveSlots.Count && remainingToRemove > 0; slotIndex++)
            {
                InventorySlot slot = inventory.LiveSlots[slotIndex];
                if (slot == null || slot.IsEmpty || slot.Count <= 0 || slot.HeldItem?.BaseItem == null)
                    continue;

                if (!string.Equals(slot.HeldItem.BaseItem.name, loss.BaseItemId, System.StringComparison.Ordinal))
                    continue;

                int amountToRemove = Mathf.Min(slot.Count, remainingToRemove);
                slot.DecreaseCount(amountToRemove);
                remainingToRemove -= amountToRemove;
                changed = true;
            }
        }

        if (changed)
        {
            inventory.NotifyInventoryUpdated();
        }
    }

    private static void ApplySavedInventoryPenaltyPlan(
        SavedInventoryData inventoryData,
        IReadOnlyList<InventoryLossEntry> losses)
    {
        if (inventoryData == null || inventoryData.Slots == null || losses == null || losses.Count == 0)
            return;

        for (int lossIndex = 0; lossIndex < losses.Count; lossIndex++)
        {
            InventoryLossEntry loss = losses[lossIndex];
            if (loss == null || loss.Count <= 0 || string.IsNullOrWhiteSpace(loss.BaseItemId))
                continue;

            int remainingToRemove = loss.Count;
            for (int slotIndex = 0; slotIndex < inventoryData.Slots.Count && remainingToRemove > 0; slotIndex++)
            {
                SavedSlotData slot = inventoryData.Slots[slotIndex];
                if (slot == null || slot.ItemData == null || slot.Count <= 0)
                    continue;

                if (!string.Equals(slot.ItemData.BaseItemID, loss.BaseItemId, System.StringComparison.Ordinal))
                    continue;

                int amountToRemove = Mathf.Min(slot.Count, remainingToRemove);
                slot.Count -= amountToRemove;
                remainingToRemove -= amountToRemove;

                if (slot.Count <= 0)
                {
                    slot.Count = 0;
                    slot.ItemData = null;
                }
            }
        }
    }

    private void ApplyRespawnResourceStateToSaveData(GameSaveData saveData)
    {
        if (saveData == null)
            return;

        PlayerStats stats = ResolvePlayerStats();
        if (stats == null)
            return;

        saveData.CurrentHealth = stats.maxHP;
        saveData.CurrentStamina = stats.maxStamina;
    }

    private static void AddOrIncrementLoss(
        List<InventoryLossEntry> losses,
        string baseItemId,
        string displayName,
        DeathPenaltyInventorySource source,
        InventoryItemSO itemBlueprint)
    {
        for (int i = 0; i < losses.Count; i++)
        {
            InventoryLossEntry existingLoss = losses[i];
            if (existingLoss != null
                && existingLoss.Source == source
                && string.Equals(existingLoss.BaseItemId, baseItemId, System.StringComparison.Ordinal))
            {
                existingLoss.Count++;
                return;
            }
        }

        losses.Add(new InventoryLossEntry(baseItemId, displayName, 1, source, itemBlueprint));
    }

    private static int CountLossItems(IReadOnlyList<InventoryLossEntry> losses)
    {
        if (losses == null)
            return 0;

        int count = 0;
        for (int i = 0; i < losses.Count; i++)
        {
            InventoryLossEntry loss = losses[i];
            if (loss != null && loss.Count > 0)
            {
                count += loss.Count;
            }
        }

        return count;
    }

    private static void AddLossSummaries(
        List<DeathItemLossSummary> summaries,
        IReadOnlyList<InventoryLossEntry> losses)
    {
        if (summaries == null || losses == null)
            return;

        for (int i = 0; i < losses.Count; i++)
        {
            InventoryLossEntry loss = losses[i];
            if (loss == null || loss.Count <= 0)
                continue;

            summaries.Add(new DeathItemLossSummary(loss.BaseItemId, loss.DisplayName, loss.Count, loss.Source, loss.ItemBlueprint));
        }
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

    private static void LoadScene(string sceneName, string loadingMessageOverride = null)
    {
        if (SceneTransitionService.Instance != null)
        {
            SceneTransitionService.Instance.LoadScene(sceneName, loadingMessageOverride);
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
            DeathPenaltyConfigSO penaltyConfig,
            DeathPenaltyPlan penaltyPlan)
        {
            MainAreaSceneName = mainAreaSceneName;
            RespawnAnchorId = respawnAnchorId;
            SaveAfterRespawnInMainArea = saveAfterRespawnInMainArea;
            PenaltyConfig = penaltyConfig;
            PenaltyPlan = penaltyPlan;
        }

        public string MainAreaSceneName { get; }
        public string RespawnAnchorId { get; }
        public bool SaveAfterRespawnInMainArea { get; }
        public DeathPenaltyConfigSO PenaltyConfig { get; }
        public DeathPenaltyPlan PenaltyPlan { get; }
    }

    private sealed class DeathPenaltyPlan
    {
        public DeathPenaltyPlan(
            DeathPenaltySummary summary,
            IReadOnlyList<InventoryLossEntry> backpackLosses,
            IReadOnlyList<InventoryLossEntry> potionLosses)
        {
            Summary = summary;
            BackpackLosses = backpackLosses;
            PotionLosses = potionLosses;
        }

        public DeathPenaltySummary Summary { get; }
        public IReadOnlyList<InventoryLossEntry> BackpackLosses { get; }
        public IReadOnlyList<InventoryLossEntry> PotionLosses { get; }
    }

    private sealed class InventoryLossCandidate
    {
        public InventoryLossCandidate(
            string baseItemId,
            string displayName,
            int count,
            DeathPenaltyInventorySource source,
            InventoryItemSO itemBlueprint)
        {
            BaseItemId = baseItemId;
            DisplayName = displayName;
            Count = count;
            Source = source;
            ItemBlueprint = itemBlueprint;
        }

        public string BaseItemId { get; }
        public string DisplayName { get; }
        public int Count { get; set; }
        public DeathPenaltyInventorySource Source { get; }
        public InventoryItemSO ItemBlueprint { get; }
    }

    private sealed class InventoryLossEntry
    {
        public InventoryLossEntry(
            string baseItemId,
            string displayName,
            int count,
            DeathPenaltyInventorySource source,
            InventoryItemSO itemBlueprint)
        {
            BaseItemId = baseItemId;
            DisplayName = displayName;
            Count = count;
            Source = source;
            ItemBlueprint = itemBlueprint;
        }

        public string BaseItemId { get; }
        public string DisplayName { get; }
        public int Count { get; set; }
        public DeathPenaltyInventorySource Source { get; }
        public InventoryItemSO ItemBlueprint { get; }
    }
}
