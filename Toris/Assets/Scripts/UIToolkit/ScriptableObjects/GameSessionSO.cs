using System;
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
        [SerializeField] private int CurrentSaveSlotIndex;
        public SaveSlotIndex ActiveSaveSlot
        {
            get => (SaveSlotIndex)CurrentSaveSlotIndex;
            set => CurrentSaveSlotIndex = (int)value;
        }
        [SerializeField] private string targetSpawnPointID;

        [Header("Skill System")]
        [SerializeField] private PlayerSkillTracker _playerSkills = new PlayerSkillTracker();
        public PlayerSkillTracker PlayerSkills => _playerSkills;

        // Specialized internal services
        private RuntimeSnapshotRegistry _snapshotRegistry = new RuntimeSnapshotRegistry();

        public static GameSessionSO LoadDefault() => Resources.Load<GameSessionSO>(DefaultResourcePath);

        public void ClearRuntimeSnapshots()
        {
            PlayerInventory = null;
            PlayerEquipment = null;
            PlayerPotionInventory = null;
            PlayerHUD = null;
            _snapshotRegistry.Clear();
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
            return SaveDataOrchestrator.Export(
                targetSpawnPointID,
                ProgressionAnchor,
                StatsAnchor,
                PlayerInventory,
                PlayerEquipment,
                PlayerPotionInventory,
                _snapshotRegistry
            );
        }

        public void ImportFromSaveData(GameSaveData saveData, ItemDatabaseSO itemDatabase)
        {
            SaveDataOrchestrator.Import(
                saveData,
                itemDatabase,
                (id) => targetSpawnPointID = id,
                ProgressionAnchor,
                StatsAnchor,
                PlayerInventory,
                PlayerEquipment,
                PlayerPotionInventory,
                _snapshotRegistry
            );
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
