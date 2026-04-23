using System;
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
        private const string DefaultResourcePath = "GameData/GameSession";

        [Header("Data References")]
        // public PlayerDataSO PlayerData; // Deprecated
        [System.NonSerialized]
        public InventoryManager PlayerInventory;

        [System.NonSerialized] private RuntimeInventorySnapshot _playerInventorySnapshot;
        [System.NonSerialized] private RuntimeInventorySnapshot _equipmentInventorySnapshot;
        [System.NonSerialized] private RuntimeProgressionSnapshot _playerProgressionSnapshot;
        [System.NonSerialized] private RuntimeStatsSnapshot _playerStatsSnapshot;
        [System.NonSerialized] private PlayerAbilitySO[] _playerAbilitySlotSnapshot;

        [Header("Save State")]
        [SerializeField] private int CurrentSaveSlotIndex;
        [SerializeField] private string targetSpawnPointID;

        [Header("Skill System")]
        [SerializeField] private PlayerSkillTracker _playerSkills = new PlayerSkillTracker();

        public PlayerSkillTracker PlayerSkills => _playerSkills;

        public static GameSessionSO LoadDefault()
        {
            return Resources.Load<GameSessionSO>(DefaultResourcePath);
        }

        public void ClearRuntimeSnapshots()
        {
            PlayerInventory = null;
            _playerInventorySnapshot = null;
            _equipmentInventorySnapshot = null;
            _playerProgressionSnapshot = null;
            _playerStatsSnapshot = null;
            _playerAbilitySlotSnapshot = null;
        }

        public void CapturePlayerInventoryState(InventoryManager inventoryManager)
        {
            _playerInventorySnapshot = RuntimeInventorySnapshot.Create(inventoryManager);
        }

        public bool TryApplyPlayerInventoryState(InventoryManager inventoryManager)
        {
            if (_playerInventorySnapshot == null)
                return false;

            _playerInventorySnapshot.ApplyTo(inventoryManager);
            _playerInventorySnapshot = null;
            return true;
        }

        public void CaptureEquipmentInventoryState(InventoryManager inventoryManager)
        {
            _equipmentInventorySnapshot = RuntimeInventorySnapshot.Create(inventoryManager);
        }

        public bool TryApplyEquipmentInventoryState(InventoryManager inventoryManager)
        {
            if (_equipmentInventorySnapshot == null)
                return false;

            _equipmentInventorySnapshot.ApplyTo(inventoryManager);
            _equipmentInventorySnapshot = null;
            return true;
        }

        public void CapturePlayerProgressionState(int level, float experience, int gold)
        {
            _playerProgressionSnapshot = new RuntimeProgressionSnapshot(level, experience, gold);
        }

        public bool TryGetPlayerProgressionState(out int level, out float experience, out int gold)
        {
            if (_playerProgressionSnapshot == null)
            {
                level = 1;
                experience = 0f;
                gold = 0;
                return false;
            }

            level = _playerProgressionSnapshot.Level;
            experience = _playerProgressionSnapshot.Experience;
            gold = _playerProgressionSnapshot.Gold;
            _playerProgressionSnapshot = null;
            return true;
        }

        public void CapturePlayerStatsState(float currentHealth, float currentStamina)
        {
            _playerStatsSnapshot = new RuntimeStatsSnapshot(currentHealth, currentStamina);
        }

        public bool TryGetPlayerStatsState(out float currentHealth, out float currentStamina)
        {
            if (_playerStatsSnapshot == null)
            {
                currentHealth = 0f;
                currentStamina = 0f;
                return false;
            }

            currentHealth = _playerStatsSnapshot.CurrentHealth;
            currentStamina = _playerStatsSnapshot.CurrentStamina;
            _playerStatsSnapshot = null;
            return true;
        }

        public void CapturePlayerAbilitySlotState(PlayerAbilitySO[] slottedAbilities)
        {
            if (slottedAbilities == null)
            {
                _playerAbilitySlotSnapshot = Array.Empty<PlayerAbilitySO>();
                return;
            }

            _playerAbilitySlotSnapshot = new PlayerAbilitySO[slottedAbilities.Length];
            Array.Copy(slottedAbilities, _playerAbilitySlotSnapshot, slottedAbilities.Length);
        }

        public bool TryGetPlayerAbilitySlotState(out PlayerAbilitySO[] slottedAbilities)
        {
            if (_playerAbilitySlotSnapshot == null)
            {
                slottedAbilities = null;
                return false;
            }

            slottedAbilities = new PlayerAbilitySO[_playerAbilitySlotSnapshot.Length];
            Array.Copy(_playerAbilitySlotSnapshot, slottedAbilities, _playerAbilitySlotSnapshot.Length);
            _playerAbilitySlotSnapshot = null;
            return true;
        }

        private sealed class RuntimeInventorySnapshot
        {
            private readonly RuntimeInventorySlotSnapshot[] _slots;

            private RuntimeInventorySnapshot(RuntimeInventorySlotSnapshot[] slots)
            {
                _slots = slots ?? Array.Empty<RuntimeInventorySlotSnapshot>();
            }

            public static RuntimeInventorySnapshot Create(InventoryManager inventoryManager)
            {
                if (inventoryManager == null || inventoryManager.LiveSlots == null)
                    return new RuntimeInventorySnapshot(Array.Empty<RuntimeInventorySlotSnapshot>());

                RuntimeInventorySlotSnapshot[] slots = new RuntimeInventorySlotSnapshot[inventoryManager.LiveSlots.Count];
                for (int i = 0; i < inventoryManager.LiveSlots.Count; i++)
                {
                    InventorySlot liveSlot = inventoryManager.LiveSlots[i];
                    if (liveSlot == null || liveSlot.IsEmpty || liveSlot.HeldItem == null || liveSlot.Count <= 0)
                    {
                        slots[i] = new RuntimeInventorySlotSnapshot(null, 0);
                        continue;
                    }

                    slots[i] = new RuntimeInventorySlotSnapshot(
                        CloneForSceneTransfer(liveSlot.HeldItem),
                        liveSlot.Count);
                }

                return new RuntimeInventorySnapshot(slots);
            }

            public void ApplyTo(InventoryManager inventoryManager)
            {
                if (inventoryManager == null || inventoryManager.LiveSlots == null)
                    return;

                int targetSlotCount = inventoryManager.LiveSlots.Count;
                for (int i = 0; i < targetSlotCount; i++)
                {
                    InventorySlot liveSlot = inventoryManager.LiveSlots[i];
                    if (liveSlot == null)
                        continue;

                    if (_slots == null || i >= _slots.Length || _slots[i] == null || _slots[i].Count <= 0 || _slots[i].Item == null)
                    {
                        liveSlot.Clear();
                        continue;
                    }

                    liveSlot.SetItem(CloneForSceneTransfer(_slots[i].Item), _slots[i].Count);
                }
            }

            private static ItemInstance CloneForSceneTransfer(ItemInstance source)
            {
                if (source == null)
                    return null;

                ItemInstance clonedItem = new ItemInstance
                {
                    InstanceID = source.InstanceID,
                    BaseItem = source.BaseItem,
                    States = new System.Collections.Generic.List<ItemComponentState>()
                };

                if (source.States != null)
                {
                    for (int i = 0; i < source.States.Count; i++)
                    {
                        ItemComponentState state = source.States[i];
                        if (state != null)
                            clonedItem.States.Add(state.Clone());
                    }
                }

                return clonedItem;
            }
        }

        private sealed class RuntimeInventorySlotSnapshot
        {
            public RuntimeInventorySlotSnapshot(ItemInstance item, int count)
            {
                Item = item;
                Count = Mathf.Max(0, count);
            }

            public ItemInstance Item { get; }
            public int Count { get; }
        }

        private sealed class RuntimeProgressionSnapshot
        {
            public RuntimeProgressionSnapshot(int level, float experience, int gold)
            {
                Level = Mathf.Max(1, level);
                Experience = Mathf.Max(0f, experience);
                Gold = Mathf.Max(0, gold);
            }

            public int Level { get; }
            public float Experience { get; }
            public int Gold { get; }
        }

        private sealed class RuntimeStatsSnapshot
        {
            public RuntimeStatsSnapshot(float currentHealth, float currentStamina)
            {
                CurrentHealth = Mathf.Max(0f, currentHealth);
                CurrentStamina = Mathf.Max(0f, currentStamina);
            }

            public float CurrentHealth { get; }
            public float CurrentStamina { get; }
        }
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
