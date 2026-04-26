using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;
using OutlandHaven.Inventory;

public class InventorySaveTester : MonoBehaviour
{
    public InventoryManager ManagerToTest;
    public MockItemDatabase ItemDatabase;

    [Header("Debug View")]
    [TextArea(10, 20)]
    public string LastSavedJson;

    private void Update()
    {
        // Press S to Save
        if (Input.GetKeyDown(KeyCode.S))
        {
            ExecuteSave();
        }

        // Press L to Load
        if (Input.GetKeyDown(KeyCode.L))
        {
            ExecuteLoad();
        }
    }

    private void ExecuteSave()
    {
        InventorySaveData saveData = new InventorySaveData();

        // Loop through the live slots in the InventoryManager
        for (int i = 0; i < ManagerToTest.LiveSlots.Count; i++)
        {
            InventorySlot liveSlot = ManagerToTest.LiveSlots[i];

            SlotSaveData slotData = new SlotSaveData { SlotIndex = i };

            // Check if the slot actually holds an item
            if (!liveSlot.IsEmpty && liveSlot.HeldItem != null && liveSlot.HeldItem.BaseItem != null)
            {
                // Extract the data
                slotData.ItemBlueprintID = liveSlot.HeldItem.BaseItem.name;
                slotData.Count = liveSlot.Count;
            }

            saveData.SavedSlots.Add(slotData);
        }

        // Serialize to a formatted JSON string for debugging
        LastSavedJson = JsonConvert.SerializeObject(saveData, Formatting.Indented);
        Debug.Log("Inventory Saved successfully!");
    }

    private void ExecuteLoad()
    {
        if (string.IsNullOrEmpty(LastSavedJson))
        {
            Debug.LogWarning("No JSON data to load!");
            return;
        }

        // Deserialize the JSON string back into our POCO
        InventorySaveData loadedData = JsonConvert.DeserializeObject<InventorySaveData>(LastSavedJson);

        // Clear existing inventory
        foreach (var slot in ManagerToTest.LiveSlots)
        {
            slot.Clear();
        }

        // Hydrate the inventory with the loaded data
        foreach (SlotSaveData loadedSlot in loadedData.SavedSlots)
        {
            if (!string.IsNullOrEmpty(loadedSlot.ItemBlueprintID))
            {
                // 1. Look up the blueprint
                InventoryItemSO blueprint = ItemDatabase.GetItemByID(loadedSlot.ItemBlueprintID);

                if (blueprint != null)
                {
                    // 2. Rebuild the runtime instance
                    ItemInstance newInstance = new ItemInstance
                    {
                        BaseItem = blueprint,
                        States = new List<ItemComponentState>()
                    };

                    // 3. Inject it back into the specific slot
                    ManagerToTest.LiveSlots[loadedSlot.SlotIndex].SetItem(newInstance, loadedSlot.Count);
                }
            }
        }

        // Trigger the UI event so the screen redraws
        ManagerToTest.NotifyInventoryUpdated();
        Debug.Log("Inventory Loaded successfully!");
    }
}