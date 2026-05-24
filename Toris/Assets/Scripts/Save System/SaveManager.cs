using Newtonsoft.Json;
using OutlandHaven.Inventory;
using OutlandHaven.UIToolkit;
using System.IO;
using UnityEngine;

namespace OutlandHaven.SaveSystem
{
    public class SaveManager : MonoBehaviour
    {
        [Header("Anchors")]
        [Tooltip("The central data hub.")]
        public GameSessionSO ActiveSession;

        [Header("Databases")]
        [Tooltip("Drag the MainItemDatabase asset here.")]
        public ItemDatabaseSO MasterItemDatabase;

        [Header("UI Events")]
        [SerializeField] private UIEventsSO _uiEvents;

        private JsonSerializerSettings _jsonSettings;
        private JsonSerializerSettings Settings
        {
            get
            {
                if (_jsonSettings == null)
                {
                    _jsonSettings = new JsonSerializerSettings
                    {
                        // All ensures that every object includes its type metadata, 
                        // which is essential for abstract/polymorphic lists like ItemComponentState.
                        TypeNameHandling = TypeNameHandling.All,
                        // ReadAhead ensures that metadata properties like $type are processed 
                        // even if they aren't the first property in the JSON object.
                        MetadataPropertyHandling = MetadataPropertyHandling.ReadAhead,
                        Formatting = Formatting.Indented
                    };
                }
                return _jsonSettings;
            }
        }

        private void OnEnable()
        {
            if (_uiEvents != null)
            {
                _uiEvents.OnQuickSaveRequested += QuickSave;
                _uiEvents.OnQuickLoadRequested += QuickLoad;
            }
        }

        private void OnDisable()
        {
            if (_uiEvents != null)
            {
                _uiEvents.OnQuickSaveRequested -= QuickSave;
                _uiEvents.OnQuickLoadRequested -= QuickLoad;
            }
        }

        // --- QUICKSAVE SYSTEM (Uses Active Slot) ---

        [ContextMenu("Execute Quick Save")]
        public void QuickSave()
        {
            if (ActiveSession == null) return;

            if (ActiveSession.IsAutoSaveBlockedForActiveSlot())
            {
#if UNITY_EDITOR
                Debug.Log($"[SaveManager] Skipped Quick Save for deleted active slot {ActiveSession.ActiveSaveSlot}.");
#endif
                return;
            }

#if UNITY_EDITOR
            Debug.Log($"[SaveManager] Quick Saving to Slot {ActiveSession.ActiveSaveSlot}...");
#endif
            SaveGame(ActiveSession.ActiveSaveSlot);
        }

        [ContextMenu("Execute Quick Load")]
        public void QuickLoad()
        {
            if (ActiveSession == null || MasterItemDatabase == null) return;

#if UNITY_EDITOR
            Debug.Log($"[SaveManager] Quick Loading from Slot {ActiveSession.ActiveSaveSlot}...");
#endif

            // 1. Read the JSON data
            GameSaveData loadedData = LoadGameData(ActiveSession.ActiveSaveSlot);

            if (loadedData != null)
            {
                // 2. Ensure the database dictionary is built
                MasterItemDatabase.Initialize();

                // 3. Push the data into the live session
                ActiveSession.ImportFromSaveData(loadedData, MasterItemDatabase);

#if UNITY_EDITOR
                Debug.Log($"[SaveManager] Slot {ActiveSession.ActiveSaveSlot} loaded successfully!");
#endif
            }
        }

        // --- SLOT SAVE SYSTEM (For Main Menu UI) ---

        public void SaveGame(SaveSlotIndex slotIndex)
        {
            if (ActiveSession == null) return;

            GameSaveData dataToSave = ActiveSession.ExportToSaveData();
            SaveGameData(slotIndex, dataToSave);
        }

        public void SaveGameData(SaveSlotIndex slotIndex, GameSaveData dataToSave)
        {
            if (dataToSave == null) return;

            string json = JsonConvert.SerializeObject(dataToSave, Settings);
            string path = GetSaveFilePath(slotIndex);

            File.WriteAllText(path, json);
        }

        public void DeleteSave(SaveSlotIndex slotIndex)
        {
            string path = GetSaveFilePath(slotIndex);
            
            if (File.Exists(path))
            {
                File.Delete(path);
#if UNITY_EDITOR
                Debug.Log($"[SaveManager] Deleted save file at: {path}");
#endif
            }

            ActiveSession?.BlockAutoSaveForDeletedSlot(slotIndex);

            // Also clean up the quicksave fallback if it exists and we're clearing slot 1
            if (slotIndex == SaveSlotIndex.Slot1)
            {
                string quickSavePath = Path.Combine(Application.persistentDataPath, "quicksave.json");
                if (File.Exists(quickSavePath))
                {
                    File.Delete(quickSavePath);
#if UNITY_EDITOR
                    Debug.Log($"[SaveManager] Deleted legacy quicksave fallback at: {quickSavePath}");
#endif
                }
            }
        }

        public GameSaveData LoadGameData(SaveSlotIndex slotIndex)
        {
            string path = GetSaveFilePath(slotIndex);

            if (!File.Exists(path))
            {
#if UNITY_EDITOR
                Debug.LogWarning("[SaveManager] No save file found at " + path);
#endif
                return null;
            }

            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<GameSaveData>(json, Settings);
        }

        /// <summary>
        /// Reads only the basic metadata from a save file without fully deserializing inventories or quest data.
        /// Uses JObject to safely bypass polymorphic type handling during the peek.
        /// </summary>
        public SaveMetadata PeekSaveMetadata(SaveSlotIndex slotIndex)
        {
            string path = GetSaveFilePath(slotIndex);
            
            if (!File.Exists(path)) return null;

            try
            {
                string json = File.ReadAllText(path);
                Newtonsoft.Json.Linq.JObject jobj = Newtonsoft.Json.Linq.JObject.Parse(json);
                
                string timestamp = jobj.GetValue("SaveTime", System.StringComparison.OrdinalIgnoreCase)?.ToString() ?? "Unknown";

                return new SaveMetadata
                {
                    SaveTime = timestamp,
                    Level = jobj.GetValue("Level", System.StringComparison.OrdinalIgnoreCase)?.ToObject<int>() ?? 0,
                    Gold = jobj.GetValue("Gold", System.StringComparison.OrdinalIgnoreCase)?.ToObject<int>() ?? 0
                };
            }
            catch (System.Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[SaveManager] Failed to peek save metadata at {path}: {ex.Message}");
#endif
                return null;
            }
        }

        private string GetSaveFilePath(SaveSlotIndex slot)
        {
            int slotNumber = (int)slot + 1;
            return Path.Combine(Application.persistentDataPath, $"save_{slotNumber}.json");
        }
    }
}
