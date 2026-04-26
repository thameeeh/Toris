using UnityEngine;
using System.Collections.Generic;
using OutlandHaven.Inventory;

public class MockItemDatabase : MonoBehaviour
{
    [Tooltip("Drag all your InventoryItemSO assets here in the inspector")]
    public List<InventoryItemSO> AllItems;

    public InventoryItemSO GetItemByID(string id)
    {
        // For this test, we'll just use the ScriptableObject's asset name as its ID
        return AllItems.Find(item => item.name == id);
    }
}