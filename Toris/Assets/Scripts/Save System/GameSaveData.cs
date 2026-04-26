using System.Collections.Generic;
using OutlandHaven.Inventory;

namespace OutlandHaven.SaveSystem
{
    [System.Serializable]
    public class GameSaveData
    {
        public string SaveTime;
        public string CurrentSceneName;
        public string SpawnPointID; // Tells the scene where to place the player

        // Progression
        public int Level;
        public float Experience;
        public int Gold;

        // Stats
        public float CurrentHealth;
        public float CurrentStamina;

        // Inventories
        public SavedInventoryData PlayerBackpack;
        public SavedInventoryData PlayerEquipment; // MISSING ADDITION
    }

    [System.Serializable]
    public class SavedInventoryData
    {
        public List<SavedSlotData> Slots = new List<SavedSlotData>();
    }

    [System.Serializable]
    public class SavedSlotData
    {
        public int SlotIndex;
        public int Count;
        public SavedItemData ItemData;
    }

    [System.Serializable]
    public class SavedItemData
    {
        public string InstanceID;
        public string BaseItemID;
        public List<ItemComponentState> States = new List<ItemComponentState>();
    }
}