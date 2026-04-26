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

        private JsonSerializerSettings _jsonSettings;
        private string _quickSavePath; // Added missing variable

        private void Awake()
        {
            // Configure Newtonsoft to handle your polymorphic ItemComponentStates
            _jsonSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                Formatting = Formatting.Indented
            };

            // Added missing initialization for the quicksave path
            _quickSavePath = Path.Combine(Application.persistentDataPath, "quicksave.json");
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F5)) QuickSave();
            if (Input.GetKeyDown(KeyCode.F9)) QuickLoad();
        }

        // --- QUICKSAVE SYSTEM (For Editor Testing) ---

        [ContextMenu("Execute Quick Save")]
        public void QuickSave()
        {
            if (ActiveSession == null)
            {
                Debug.LogError("[SaveManager] ActiveSession is missing! Drag the GameSessionSO into the inspector.");
                return;
            }

            Debug.Log("[SaveManager] Starting Quick Save...");

            // 1. Get the pure data from the Session
            GameSaveData dataToSave = ActiveSession.ExportToSaveData();

            // 2. Convert to JSON text
            string json = JsonConvert.SerializeObject(dataToSave, _jsonSettings);

            // 3. Write to Hard Drive
            File.WriteAllText(_quickSavePath, json);

            Debug.Log($"[SaveManager] Quicksave successful! File located at: {_quickSavePath}");
        }

        [ContextMenu("Execute Quick Load")]
        public void QuickLoad()
        {
            if (ActiveSession == null || MasterItemDatabase == null)
            {
                Debug.LogError("[SaveManager] Missing references! Check the Inspector.");
                return;
            }

            if (!File.Exists(_quickSavePath))
            {
                Debug.LogWarning("[SaveManager] No quicksave file found to load!");
                return;
            }

            Debug.Log("[SaveManager] Starting Quick Load...");

            // 1. Read the JSON string from the hard drive
            string json = File.ReadAllText(_quickSavePath);

            // 2. Deserialize it back into the pure C# DTO
            GameSaveData loadedData = JsonConvert.DeserializeObject<GameSaveData>(json, _jsonSettings);

            if (loadedData != null)
            {
                // 3. Ensure the database dictionary is built before we try to look up items
                MasterItemDatabase.Initialize();

                // 4. Push the data into the live session
                ActiveSession.ImportFromSaveData(loadedData, MasterItemDatabase);

                Debug.Log("[SaveManager] Quicksave loaded successfully!");
            }
        }

        // --- SLOT SAVE SYSTEM (For Main Menu UI) ---

        public void SaveGame(SaveSlotIndex slotIndex)
        {
            if (ActiveSession == null) return;

            GameSaveData dataToSave = ActiveSession.ExportToSaveData();
            string json = JsonConvert.SerializeObject(dataToSave, _jsonSettings);
            string path = GetSaveFilePath(slotIndex);

            File.WriteAllText(path, json);

            Debug.Log($"[SaveManager] Game Saved successfully to {path}");
        }

        public GameSaveData LoadGameData(SaveSlotIndex slotIndex)
        {
            string path = GetSaveFilePath(slotIndex);
            if (!File.Exists(path))
            {
                Debug.LogWarning("[SaveManager] No save file found at " + path);
                return null;
            }

            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<GameSaveData>(json, _jsonSettings);
        }

        private string GetSaveFilePath(SaveSlotIndex slot)
        {
            return Path.Combine(Application.persistentDataPath, $"save_{slot}.json");
        }
    }
}