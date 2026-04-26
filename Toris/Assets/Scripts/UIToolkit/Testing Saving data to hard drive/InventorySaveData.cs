using System.Collections.Generic;

[System.Serializable]
public class InventorySaveData
{
    public List<SlotSaveData> SavedSlots = new List<SlotSaveData>();
}

[System.Serializable]
public class SlotSaveData
{
    public int SlotIndex;
    public string ItemBlueprintID;
    public int Count;
}
