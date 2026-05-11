using UnityEngine;
using OutlandHaven.Inventory;

#if UNITY_EDITOR
using UnityEditor;
using System.Collections.Generic;
#endif

namespace OutlandHaven.Core
{
    public class UIBootstraper : MonoBehaviour
    {
        [Header("Global System Managers")]
        [Tooltip("Drop your ScriptableObject managers here to force them to load and listen to events.")]
        [SerializeField] private ScriptableObject[] _persistentManagers;

        [Header("Item Database Sync (Editor Only)")]
        [SerializeField] private ItemDatabaseSO _itemDatabase;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_itemDatabase == null)
            {
                Debug.LogError($"[UIBootstraper] Missing required asset: <color=yellow>ItemDatabaseSO</color>!", this);
            }
            else
            {
                // Ensure the database is always up to date
                _itemDatabase.GatherAllItems();
            }
        }
#endif
    }
}