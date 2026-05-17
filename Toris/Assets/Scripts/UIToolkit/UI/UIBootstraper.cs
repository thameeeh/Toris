using UnityEngine;
using OutlandHaven.Inventory;
using OutlandHaven.UIToolkit;


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

        [Header("Runtime Binding")]
        [SerializeField] private GameSessionSO _globalSession;
        [SerializeField] private UIEventsSO _uiEvents;

        private void Start()
        {
            InitializeRuntimeBindings();
        }

        private void InitializeRuntimeBindings()
        {
            if (_globalSession == null)
            {
                _globalSession = GameSessionSO.LoadDefault();
            }

            // GlobalSession bindings are now handled automatically by the 
            // InventoryManager instances themselves during their OnEnable lifecycle.
            // UIBootstraper remains responsible for ensuring the GameSession is loaded
            // and broadcasting the initialization completion event.

            // Notify UI systems that the scene's core references are now ready
            if (_uiEvents != null)
            {
                _uiEvents.OnSystemInitializationComplete?.Invoke();
            }
        }
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