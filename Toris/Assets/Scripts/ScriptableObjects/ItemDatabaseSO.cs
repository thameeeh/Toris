using System.Collections.Generic;
using UnityEngine;

namespace OutlandHaven.Inventory
{
    [CreateAssetMenu(menuName = "Data/Item Database")]
    public class ItemDatabaseSO : ScriptableObject
    {
        [Tooltip("Master list of all item blueprints in the game.")]
        public List<InventoryItemSO> AllItems = new List<InventoryItemSO>();

        // Runtime dictionary for instant O(1) lookups, completely avoiding slow list iteration.
        private Dictionary<string, InventoryItemSO> _itemRegistry;

        /// <summary>
        /// Builds the dictionary. Must be called before loading a save file.
        /// </summary>
        public void Initialize()
        {
            if (_itemRegistry != null) return; // Prevent double initialization

            _itemRegistry = new Dictionary<string, InventoryItemSO>();

            foreach (var item in AllItems)
            {
                if (item != null)
                {
                    // Using the asset's file name (item.name) as the unique ID
                    if (!_itemRegistry.ContainsKey(item.name))
                    {
                        _itemRegistry.Add(item.name, item);
                    }
                    else
                    {
                        Debug.LogError($"[ItemDatabase] Duplicate item ID found: {item.name}. Ensure all item assets have unique file names.");
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves the blueprint blueprint using the string ID from the save file.
        /// </summary>
        public InventoryItemSO GetItemByID(string itemID)
        {
            if (_itemRegistry == null) Initialize();

            if (_itemRegistry.TryGetValue(itemID, out InventoryItemSO item))
            {
                return item;
            }

            Debug.LogError($"[ItemDatabase] Failed to find item ID: {itemID}. Is it missing from the database list?");
            return null;
        }

#if UNITY_EDITOR
        // This is an Editor-only convenience tool so you don't have to manually drag 
        // hundreds of items into the list one by one.
        [ContextMenu("Auto-Populate Database")]
        public void GatherAllItems()
        {
            AllItems.Clear();
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:InventoryItemSO");

            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                InventoryItemSO item = UnityEditor.AssetDatabase.LoadAssetAtPath<InventoryItemSO>(path);
                if (item != null) AllItems.Add(item);
            }

            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[ItemDatabase] Auto-populated {AllItems.Count} items.");
        }
#endif
    }
}