using System;
using System.Collections.Generic;
using UnityEngine;
using OutlandHaven.Inventory;

namespace OutlandHaven.SaveSystem
{
    /// <summary>
    /// Responsible for capturing and restoring volatile runtime state during scene transitions.
    /// This data is NOT serialized to disk; it lives in memory to bridge scene loads.
    /// </summary>
    public class RuntimeSnapshotRegistry
    {
        private RuntimeInventorySnapshot _playerInventorySnapshot;
        private RuntimeInventorySnapshot _equipmentInventorySnapshot;
        private RuntimeInventorySnapshot _potionInventorySnapshot;
        private RuntimeProgressionSnapshot _playerProgressionSnapshot;
        private RuntimeStatsSnapshot _playerStatsSnapshot;
        private PlayerAbilitySO[] _playerAbilitySlotSnapshot;

        public void Clear()
        {
            _playerInventorySnapshot = null;
            _equipmentInventorySnapshot = null;
            _potionInventorySnapshot = null;
            _playerProgressionSnapshot = null;
            _playerStatsSnapshot = null;
            _playerAbilitySlotSnapshot = null;
        }

        #region Inventory Snapshots
        public RuntimeInventorySnapshot PlayerInventory => _playerInventorySnapshot;
        public RuntimeInventorySnapshot EquipmentInventory => _equipmentInventorySnapshot;
        public RuntimeInventorySnapshot PotionInventory => _potionInventorySnapshot;

        public void CapturePlayerInventory(InventoryManager manager) => _playerInventorySnapshot = RuntimeInventorySnapshot.Create(manager);
        public void CaptureEquipmentInventory(InventoryManager manager) => _equipmentInventorySnapshot = RuntimeInventorySnapshot.Create(manager);
        public void CapturePotionInventory(InventoryManager manager) => _potionInventorySnapshot = RuntimeInventorySnapshot.Create(manager);

        public void CapturePlayerInventory(SavedInventoryData data, ItemDatabaseSO database) => _playerInventorySnapshot = RuntimeInventorySnapshot.CreateFromSavedData(data, database);
        public void CaptureEquipmentInventory(SavedInventoryData data, ItemDatabaseSO database) => _equipmentInventorySnapshot = RuntimeInventorySnapshot.CreateFromSavedData(data, database);
        public void CapturePotionInventory(SavedInventoryData data, ItemDatabaseSO database) => _potionInventorySnapshot = RuntimeInventorySnapshot.CreateFromSavedData(data, database);

        public bool ApplyPlayerInventory(InventoryManager manager) => ApplySnapshot(_playerInventorySnapshot, manager);
        public bool ApplyEquipmentInventory(InventoryManager manager) => ApplySnapshot(_equipmentInventorySnapshot, manager);
        public bool ApplyPotionInventory(InventoryManager manager) => ApplySnapshot(_potionInventorySnapshot, manager);

        private bool ApplySnapshot(RuntimeInventorySnapshot snapshot, InventoryManager manager)
        {
            if (snapshot == null) return false;
            snapshot.ApplyTo(manager);
            return true;
        }
        #endregion

        #region Progression & Stats
        public void CaptureProgression(int level, float experience, int gold)
        {
            _playerProgressionSnapshot = new RuntimeProgressionSnapshot(level, experience, gold);
        }

        public bool TryGetProgression(out int level, out float experience, out int gold)
        {
            if (_playerProgressionSnapshot == null)
            {
                level = 1; experience = 0f; gold = 0;
                return false;
            }
            level = _playerProgressionSnapshot.Level;
            experience = _playerProgressionSnapshot.Experience;
            gold = _playerProgressionSnapshot.Gold;
            return true;
        }

        public void CaptureStats(float health, float stamina)
        {
            _playerStatsSnapshot = new RuntimeStatsSnapshot(health, stamina);
        }

        public bool TryGetStats(out float health, out float stamina)
        {
            if (_playerStatsSnapshot == null)
            {
                health = 0f; stamina = 0f;
                return false;
            }
            health = _playerStatsSnapshot.CurrentHealth;
            stamina = _playerStatsSnapshot.CurrentStamina;
            return true;
        }
        #endregion

        #region Abilities
        public void CaptureAbilities(PlayerAbilitySO[] slotted)
        {
            if (slotted == null) { _playerAbilitySlotSnapshot = Array.Empty<PlayerAbilitySO>(); return; }
            _playerAbilitySlotSnapshot = new PlayerAbilitySO[slotted.Length];
            Array.Copy(slotted, _playerAbilitySlotSnapshot, slotted.Length);
        }

        public bool TryGetAbilities(out PlayerAbilitySO[] slotted)
        {
            if (_playerAbilitySlotSnapshot == null) { slotted = null; return false; }
            slotted = new PlayerAbilitySO[_playerAbilitySlotSnapshot.Length];
            Array.Copy(_playerAbilitySlotSnapshot, slotted, _playerAbilitySlotSnapshot.Length);
            _playerAbilitySlotSnapshot = null;
            return true;
        }
        #endregion

        #region Snapshot Data Structures
        public sealed class RuntimeInventorySnapshot
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

                    slots[i] = new RuntimeInventorySlotSnapshot(CloneForSceneTransfer(liveSlot.HeldItem), liveSlot.Count);
                }
                return new RuntimeInventorySnapshot(slots);
            }

            public static RuntimeInventorySnapshot CreateFromSavedData(SavedInventoryData data, ItemDatabaseSO database)
            {
                if (data == null || data.Slots == null || database == null)
                    return new RuntimeInventorySnapshot(Array.Empty<RuntimeInventorySlotSnapshot>());

                int maxIndex = -1;
                foreach (var s in data.Slots) if (s.SlotIndex > maxIndex) maxIndex = s.SlotIndex;

                RuntimeInventorySlotSnapshot[] snapshots = new RuntimeInventorySlotSnapshot[maxIndex + 1];
                foreach (var savedSlot in data.Slots)
                {
                    if (savedSlot.ItemData != null && savedSlot.Count > 0)
                    {
                        InventoryItemSO blueprint = database.GetItemByID(savedSlot.ItemData.BaseItemID);
                        if (blueprint != null)
                        {
                            ItemInstance item = new ItemInstance
                            {
                                InstanceID = savedSlot.ItemData.InstanceID,
                                BaseItem = blueprint,
                                States = savedSlot.ItemData.States ?? new List<ItemComponentState>()
                            };
                            snapshots[savedSlot.SlotIndex] = new RuntimeInventorySlotSnapshot(item, savedSlot.Count);
                        }
                    }
                }
                return new RuntimeInventorySnapshot(snapshots);
            }

            public void ApplyTo(InventoryManager inventoryManager)
            {
                if (inventoryManager == null || inventoryManager.LiveSlots == null) return;

                int targetSlotCount = inventoryManager.LiveSlots.Count;
                for (int i = 0; i < targetSlotCount; i++)
                {
                    InventorySlot liveSlot = inventoryManager.LiveSlots[i];
                    if (liveSlot == null) continue;

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
                if (source == null) return null;
                ItemInstance clonedItem = new ItemInstance
                {
                    InstanceID = source.InstanceID,
                    BaseItem = source.BaseItem,
                    States = new List<ItemComponentState>()
                };

                if (source.States != null)
                {
                    foreach (var state in source.States)
                    {
                        if (state != null) clonedItem.States.Add(state.Clone());
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
        #endregion
    }
}
