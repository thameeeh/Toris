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

            // Resolve and bind primary inventories to prevent race conditions during scene transitions
            InventoryManager backpack = PlayerInventorySceneResolver.ResolvePlayerInventory(this, null);
            InventoryManager equipment = PlayerInventorySceneResolver.ResolveEquipmentInventory(this, null, backpack);
            InventoryManager potions = PlayerInventorySceneResolver.ResolvePotionInventory(null);

            if (_globalSession != null)
            {
                _globalSession.PlayerInventory = backpack;
                _globalSession.PlayerEquipment = equipment;
                _globalSession.PlayerPotionInventory = potions;

                Debug.Log("[UI/Inventory] UIBootstraper: GlobalSession bindings established.");
            }

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