using System;
using System.Collections.Generic;
using OutlandHaven.SaveSystem;
using UnityEngine;
using OutlandHaven.Inventory;

namespace OutlandHaven.UIToolkit
{
    public enum SaveSlotIndex
    {
        Slot1 = 0,
        Slot2 = 1,
        Slot3 = 2
    }

    public enum PlayerClass
    {
        Archer,
        Warrior, 
        Mage
    }

    [CreateAssetMenu(menuName = "UI/Scriptable Objects/GameSessionSO")]
    public class GameSessionSO : ScriptableObject
    {
        /*
        #########################################################################################
        #                                                                                       #
        #   GAMESESSIONSO ARCHITECTURE: FACADE & SERVICE DELEGATION                             #
        #                                                                                       #
        #   1. RuntimeSnapshotRegistry: Handles volatile, in-memory data for scene transitions. #
        #      - Purpose: Bridges state when the player moves between Unity scenes.             #
        #                                                                                       #
        #   2. SaveDataOrchestrator: Handles persistence logic (JSON IO & DTO Mapping).        #
        #      - Purpose: Converts live game state into a serializable SaveData format.         #
        #                                                                                       #
        #########################################################################################
        */

        private const string DefaultResourcePath = "GameData/GameSession";

        [Header("Data References")]
        [System.NonSerialized] public InventoryManager PlayerInventory;
        [System.NonSerialized] public InventoryManager PlayerEquipment;
        [System.NonSerialized] public InventoryManager PlayerPotionInventory;
        [System.NonSerialized] public PlayerHUDBridge PlayerHUD;

        [Header("Global Anchors")]
        public PlayerProgressionAnchorSO ProgressionAnchor;
        public PlayerStatsAnchorSO StatsAnchor;

        [Header("Save State")]
        [System.NonSerialized] private SaveSlotIndex _activeSaveSlot = SaveSlotIndex.Slot1;
        [System.NonSerialized] private SaveSlotIndex? _autoSaveBlockedSlot;
        public SaveSlotIndex ActiveSaveSlot
        {
            get => _activeSaveSlot;
            set => _activeSaveSlot = value;
        }
        [SerializeField] private string targetSpawnPointID;

        [Header("Skill System")]
        [SerializeField] private PlayerSkillTracker _playerSkills = new PlayerSkillTracker();
        public PlayerSkillTracker PlayerSkills => _playerSkills;

        [Header("Tutorial")]
        [SerializeField] private bool _tutorialsEnabled = true;
        private readonly HashSet<string> _completedTutorialStepIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public bool TutorialsEnabled => _tutorialsEnabled;

        // Specialized internal services
        private RuntimeSnapshotRegistry _snapshotRegistry = new RuntimeSnapshotRegistry();

        public static GameSessionSO LoadDefault() => Resources.Load<GameSessionSO>(DefaultResourcePath);

        public void ClearRuntimeSnapshots()
        {
            PlayerInventory = null;
            PlayerEquipment = null;
            PlayerPotionInventory = null;
            PlayerHUD = null;
            targetSpawnPointID = string.Empty;
            _snapshotRegistry.Clear();
            _playerSkills.Reset();
            StartNewTutorialState(tutorialsEnabled: true);
        }

        public void PrepareNewGame(bool tutorialsEnabled)
        {
            ClearRuntimeSnapshots();
            StartNewTutorialState(tutorialsEnabled);
            global::PixelCrushersDialogueSaveBridge.RequestResetForNewGame();
        }

        public void StartNewTutorialState(bool tutorialsEnabled)
        {
            _tutorialsEnabled = tutorialsEnabled;
            _completedTutorialStepIds.Clear();
        }

        public bool IsAutoSaveBlockedForActiveSlot()
        {
            return _autoSaveBlockedSlot.HasValue && _autoSaveBlockedSlot.Value == ActiveSaveSlot;
        }

        public void BlockAutoSaveForDeletedSlot(SaveSlotIndex slotIndex)
        {
            if (slotIndex == ActiveSaveSlot)
                _autoSaveBlockedSlot = slotIndex;
        }

        public void AllowAutoSaveForSlot(SaveSlotIndex slotIndex)
        {
            if (_autoSaveBlockedSlot.HasValue && _autoSaveBlockedSlot.Value == slotIndex)
                _autoSaveBlockedSlot = null;
        }

        public bool IsTutorialStepCompleted(string stepId)
        {
            return !string.IsNullOrWhiteSpace(stepId)
                && _completedTutorialStepIds.Contains(stepId);
        }

        public void MarkTutorialStepCompleted(string stepId)
        {
            if (!string.IsNullOrWhiteSpace(stepId))
            {
                _completedTutorialStepIds.Add(stepId.Trim());
            }
        }

        public SavedTutorialProgressData ExportTutorialProgress()
        {
            // Persistence bridge only: keep tutorial flow/trigger logic in Scripts/Tutorial.
            return new SavedTutorialProgressData
            {
                TutorialsEnabled = _tutorialsEnabled,
                CompletedStepIds = new List<string>(_completedTutorialStepIds)
            };
        }

        private void ImportTutorialProgress(SavedTutorialProgressData data)
        {
            _completedTutorialStepIds.Clear();

            if (data == null)
            {
                _tutorialsEnabled = true;
                return;
            }

            _tutorialsEnabled = data.TutorialsEnabled;

            if (data.CompletedStepIds == null)
                return;

            for (int i = 0; i < data.CompletedStepIds.Count; i++)
            {
                string stepId = data.CompletedStepIds[i];
                if (!string.IsNullOrWhiteSpace(stepId))
                {
                    _completedTutorialStepIds.Add(stepId.Trim());
                }
            }
        }

        #region Facade: Snapshot Delegation
        public void CapturePlayerInventoryState(InventoryManager manager) => _snapshotRegistry.CapturePlayerInventory(manager);
        public void CapturePlayerInventoryState(SavedInventoryData data, ItemDatabaseSO db) => _snapshotRegistry.CapturePlayerInventory(data, db);
        public bool TryApplyPlayerInventoryState(InventoryManager manager) => _snapshotRegistry.ApplyPlayerInventory(manager);

        public void CaptureEquipmentInventoryState(InventoryManager manager) => _snapshotRegistry.CaptureEquipmentInventory(manager);
        public void CaptureEquipmentInventoryState(SavedInventoryData data, ItemDatabaseSO db) => _snapshotRegistry.CaptureEquipmentInventory(data, db);
        public bool TryApplyEquipmentInventoryState(InventoryManager manager) => _snapshotRegistry.ApplyEquipmentInventory(manager);

        public void CapturePotionInventoryState(InventoryManager manager) => _snapshotRegistry.CapturePotionInventory(manager);
        public void CapturePotionInventoryState(SavedInventoryData data, ItemDatabaseSO db) => _snapshotRegistry.CapturePotionInventory(data, db);
        public bool TryApplyPotionInventoryState(InventoryManager manager) => _snapshotRegistry.ApplyPotionInventory(manager);

        // Debugging accessors
        public object GetPlayerInventorySnapshot() => _snapshotRegistry.PlayerInventory;
        public object GetEquipmentInventorySnapshot() => _snapshotRegistry.EquipmentInventory;
        public object GetPotionInventorySnapshot() => _snapshotRegistry.PotionInventory;

        public void CapturePlayerProgressionState(int lvl, float exp, int gold) => _snapshotRegistry.CaptureProgression(lvl, exp, gold);
        public bool TryGetPlayerProgressionState(out int lvl, out float exp, out int gold) => _snapshotRegistry.TryGetProgression(out lvl, out exp, out gold);

        public void CapturePlayerStatsState(float hp, float stamina) => _snapshotRegistry.CaptureStats(hp, stamina);
        public bool TryGetPlayerStatsState(out float hp, out float stamina) => _snapshotRegistry.TryGetStats(out hp, out stamina);

        public void CapturePlayerAbilitySlotState(PlayerAbilitySO[] slotted) => _snapshotRegistry.CaptureAbilities(slotted);
        public bool TryGetPlayerAbilitySlotState(out PlayerAbilitySO[] slotted) => _snapshotRegistry.TryGetAbilities(out slotted);
        #endregion

        #region Facade: Persistence Delegation
        public GameSaveData ExportToSaveData()
        {
            GameSaveData saveData = SaveDataOrchestrator.Export(
                targetSpawnPointID,
                ProgressionAnchor,
                StatsAnchor,
                PlayerInventory,
                PlayerEquipment,
                PlayerPotionInventory,
                _playerSkills,
                _snapshotRegistry
            );

            saveData.TutorialProgress = ExportTutorialProgress();
            return saveData;
        }

        public void ImportFromSaveData(GameSaveData saveData, ItemDatabaseSO itemDatabase)
        {
            if (saveData == null)
                return;

            SaveDataOrchestrator.Import(
                saveData,
                itemDatabase,
                (id) => targetSpawnPointID = id,
                ProgressionAnchor,
                StatsAnchor,
                PlayerInventory,
                PlayerEquipment,
                PlayerPotionInventory,
                _playerSkills,
                _snapshotRegistry
            );

            ImportTutorialProgress(saveData.TutorialProgress);
        }
        #endregion
    }

    internal static class GameSessionRuntimeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ResetRuntimeSessionState()
        {
            GameSessionSO defaultSession = GameSessionSO.LoadDefault();
            if (defaultSession != null)
            {
                defaultSession.ClearRuntimeSnapshots();
            }
        }
    }
}
