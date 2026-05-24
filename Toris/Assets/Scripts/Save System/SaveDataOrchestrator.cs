using System;
using System.Collections.Generic;
using UnityEngine;
using OutlandHaven.Inventory;

namespace OutlandHaven.SaveSystem
{
    /// <summary>
    /// Responsible for the heavy lifting of converting live GameSession data 
    /// into a serializable GameSaveData DTO and vice versa.
    /// This is the "Hard Drive" specialist.
    /// </summary>
    public static class SaveDataOrchestrator
    {
        public static GameSaveData Export(
            string spawnPointID,
            PlayerProgressionAnchorSO progressionAnchor,
            PlayerStatsAnchorSO statsAnchor,
            InventoryManager playerInventory,
            InventoryManager equipmentInventory,
            InventoryManager potionInventory,
            PlayerSkillTracker skillTracker,
            RuntimeSnapshotRegistry snapshotRegistry,
            GameplayStatisticsSO gameplayStatistics = null)
        {
            GameSaveData saveData = new GameSaveData();
            saveData.SaveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            saveData.SpawnPointID = spawnPointID;

            // --- 1. EXPORT PROGRESSION ---
            if (progressionAnchor != null && progressionAnchor.IsReady)
            {
                saveData.Level = progressionAnchor.Instance.CurrentLevel;
                saveData.Experience = progressionAnchor.Instance.CurrentExperience;
                saveData.Gold = progressionAnchor.Instance.CurrentGold;
            }
            else if (snapshotRegistry.TryGetProgression(out int level, out float exp, out int gold))
            {
                saveData.Level = level;
                saveData.Experience = exp;
                saveData.Gold = gold;
                // Re-capture to keep the snapshot alive if needed
                snapshotRegistry.CaptureProgression(level, exp, gold);
            }
            else
            {
                saveData.Level = 1;
            }

            // --- 2. EXPORT STATS ---
            if (statsAnchor != null && statsAnchor.IsReady)
            {
                saveData.CurrentHealth = statsAnchor.Instance.currentHP;
                saveData.CurrentStamina = statsAnchor.Instance.currentStamina;
            }
            else if (snapshotRegistry.TryGetStats(out float currentHealth, out float currentStamina))
            {
                saveData.CurrentHealth = currentHealth;
                saveData.CurrentStamina = currentStamina;
                snapshotRegistry.CaptureStats(currentHealth, currentStamina);
            }

            // --- 3. EXPORT INVENTORIES ---
            saveData.PlayerBackpack = ExtractInventoryData(playerInventory);
            saveData.PlayerEquipment = ExtractInventoryData(equipmentInventory);
            saveData.PlayerPotion = ExtractInventoryData(potionInventory);

            // --- 4. EXPORT SKILLS ---
            if (skillTracker != null)
            {
                saveData.SkillProgress = new SavedSkillProgressData
                {
                    AvailableSP = skillTracker.AvailableSP,
                    UnlockedSkillIDs = new List<string>(skillTracker.UnlockedSkillIDs)
                };
            }

            saveData.PixelCrushersDialogueSaveData = global::PixelCrushersDialogueSaveBridge.CaptureSaveData();

            // --- 6. EXPORT GAMEPLAY STATISTICS ---
            if (gameplayStatistics != null)
            {
                saveData.GameplayStatistics = gameplayStatistics.CaptureToSaveData();
            }

            return saveData;
        }

        public static void Import(
            GameSaveData saveData, 
            ItemDatabaseSO itemDatabase,
            Action<string> setSpawnPoint,
            PlayerProgressionAnchorSO progressionAnchor,
            PlayerStatsAnchorSO statsAnchor,
            InventoryManager playerInventory,
            InventoryManager equipmentInventory,
            InventoryManager potionInventory,
            PlayerSkillTracker skillTracker,
            RuntimeSnapshotRegistry snapshotRegistry,
            GameplayStatisticsSO gameplayStatistics = null)
        {
            if (saveData == null) return;

            setSpawnPoint?.Invoke(saveData.SpawnPointID);

            // --- 1. RESTORE STATS & PROGRESSION ---
            snapshotRegistry.CaptureProgression(saveData.Level, saveData.Experience, saveData.Gold);
            snapshotRegistry.CaptureStats(saveData.CurrentHealth, saveData.CurrentStamina);

            if (progressionAnchor != null && progressionAnchor.IsReady)
            {
                progressionAnchor.Instance.SetRuntimeState(saveData.Level, saveData.Experience, saveData.Gold);
            }

            if (statsAnchor != null && statsAnchor.IsReady)
            {
                statsAnchor.Instance.SetRuntimeState(saveData.CurrentHealth, saveData.CurrentStamina);
            }

            // --- 2. RESTORE INVENTORIES ---
            RestoreOrSnapshot(playerInventory, saveData.PlayerBackpack, itemDatabase, snapshotRegistry.CapturePlayerInventory);
            RestoreOrSnapshot(equipmentInventory, saveData.PlayerEquipment, itemDatabase, snapshotRegistry.CaptureEquipmentInventory);
            RestoreOrSnapshot(potionInventory, saveData.PlayerPotion, itemDatabase, snapshotRegistry.CapturePotionInventory);

            // --- 3. RESTORE SKILL PROGRESSION ---
            if (skillTracker != null && saveData.SkillProgress != null)
            {
                skillTracker.LoadState(saveData.SkillProgress.AvailableSP, saveData.SkillProgress.UnlockedSkillIDs);
            }

            global::PixelCrushersDialogueSaveBridge.RequestApplySaveData(saveData.PixelCrushersDialogueSaveData);

            // --- 4. RESTORE GAMEPLAY STATISTICS ---
            if (gameplayStatistics != null)
            {
                gameplayStatistics.RestoreFromSaveData(saveData.GameplayStatistics);
            }
        }

        private static void RestoreOrSnapshot(
            InventoryManager manager, 
            SavedInventoryData data, 
            ItemDatabaseSO database, 
            Action<SavedInventoryData, ItemDatabaseSO> snapshotAction)
        {
            if (data == null) return;
            if (manager != null)
                RestoreInventoryData(manager, data, database);
            else
                snapshotAction?.Invoke(data, database);
        }

        private static SavedInventoryData ExtractInventoryData(InventoryManager inventory)
        {
            if (inventory == null || inventory.LiveSlots == null) return null;

            SavedInventoryData savedData = new SavedInventoryData();
            for (int i = 0; i < inventory.LiveSlots.Count; i++)
            {
                var liveSlot = inventory.LiveSlots[i];
                SavedSlotData slotData = new SavedSlotData { SlotIndex = i, Count = liveSlot.Count };

                if (!liveSlot.IsEmpty)
                {
                    slotData.ItemData = new SavedItemData
                    {
                        InstanceID = liveSlot.HeldItem.InstanceID,
                        BaseItemID = liveSlot.HeldItem.BaseItem.name,
                        States = liveSlot.HeldItem.States
                    };
                }
                savedData.Slots.Add(slotData);
            }
            return savedData;
        }

        private static void RestoreInventoryData(InventoryManager inventory, SavedInventoryData savedData, ItemDatabaseSO itemDatabase)
        {
            if (inventory == null || inventory.LiveSlots == null || savedData == null) return;

            foreach (var slot in inventory.LiveSlots) slot.Clear();

            foreach (var savedSlot in savedData.Slots)
            {
                if (savedSlot.SlotIndex >= inventory.LiveSlots.Count) continue;

                InventorySlot liveSlot = inventory.LiveSlots[savedSlot.SlotIndex];

                if (savedSlot.ItemData != null && savedSlot.Count > 0)
                {
                    InventoryItemSO blueprint = itemDatabase.GetItemByID(savedSlot.ItemData.BaseItemID);
                    if (blueprint != null)
                    {
                        ItemInstance restoredItem = new ItemInstance
                        {
                            InstanceID = savedSlot.ItemData.InstanceID,
                            BaseItem = blueprint,
                            States = savedSlot.ItemData.States ?? new List<ItemComponentState>()
                        };
                        liveSlot.SetItem(restoredItem, savedSlot.Count);
                    }
                }
            }
            inventory.NotifyInventoryUpdated();
        }
    }
}
