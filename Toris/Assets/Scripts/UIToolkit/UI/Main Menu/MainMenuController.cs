using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using OutlandHaven.UIToolkit;
using OutlandHaven.SaveSystem;

public class MainMenuController : MonoBehaviour
{
    [Header("Templates")]
    [SerializeField] private VisualTreeAsset _mainMenuTemplate;
    [SerializeField] private VisualTreeAsset _saveSlotTemplate; // Added for the Slot Cards

    [Header("Dependencies")]
    [SerializeField] private UIEventsSO _uiEvents;
    [SerializeField] private SaveManager _saveManager;

    private MainMenuView _view;
    private MainMenuUIManager _uiManager;

    private bool _slotsGenerated = false;

    private void Awake()
    {
        _uiManager = FindFirstObjectByType<MainMenuUIManager>();
        
        // Ensure we have a SaveManager if not manually assigned
        if (_saveManager == null)
        {
            _saveManager = FindFirstObjectByType<SaveManager>();
        }
    }

    private void Start()
    {
        if (_mainMenuTemplate == null || _uiManager == null) return;

        // 1. Instantiate the Main Menu UXML
        TemplateContainer menuInstance = _mainMenuTemplate.Instantiate();
        menuInstance.style.flexGrow = 1;

        // 2. Construct and Initialize the View
        _view = new MainMenuView(menuInstance, _uiEvents);
        _view.SetSaveSlotTemplate(_saveSlotTemplate);
        _view.Initialize();

        // 3. Subscribe to the Main Menu buttons
        _view.OnPlayClicked += OnPlayRequested;
        _view.OnSettingsClicked += OnSettingsRequested;
        _view.OnExitClicked += OnExitRequested;
        _view.OnSaveSlotSelected += HandleSlotSelected;

        // 4. Register the view with the Manager
        _uiManager.RegisterView(_view);
    }

    private void GenerateMockSaveSlots()
    {
        List<SaveSlotData> mockSlots = new List<SaveSlotData>();

        for (int i = 1; i <= 3; i++)
        {
            mockSlots.Add(new SaveSlotData
            {
                SlotIndex = i,
                Level = i * 5,
                Gold = i * 1250,
                Timestamp = $"2026-05-0{i + 4} 14:30"
            });
        }

        _view.PopulateSaveSlots(mockSlots);
    }

    // --- Intent Handlers ---

    private void HandleSlotSelected(int slotIndex)
    {
        if (_saveManager == null)
        {
            Debug.LogError("[MainMenuController] SaveManager reference is missing! Cannot load game.");
            return;
        }

        // Convert the 1-based UI index back to the 0-based Enum
        SaveSlotIndex enumIndex = (SaveSlotIndex)(slotIndex - 1);

        Debug.Log($"UI Intent: Selected Save Slot {slotIndex} ({enumIndex}). Requesting Load...");

        // 1. Load the data
        GameSaveData loadedData = _saveManager.LoadGameData(enumIndex);

        if (loadedData != null)
        {
            // 2. Apply to session
            _saveManager.ActiveSession.ImportFromSaveData(loadedData, _saveManager.MasterItemDatabase);

            // 3. Transition Scene
            string sceneToLoad = string.IsNullOrEmpty(loadedData.CurrentSceneName) ? "MainArea" : loadedData.CurrentSceneName;
            SceneTransitionService.Instance.LoadScene(sceneToLoad);
        }
        else
        {
            Debug.Log($"[MainMenuController] Slot {slotIndex} is empty. Initializing New Game sequence...");

            // 1. Reset the session to default state
            _saveManager.ActiveSession.ClearRuntimeSnapshots();

            // 2. Load the starting scene
            SceneTransitionService.Instance.LoadScene("MainArea");
        }
    }

    private void OnPlayRequested()
    {
        Debug.Log("UI Intent: Play Clicked. (Slots are already visible on the right).");

        if (!_slotsGenerated)
        {
            GenerateMockSaveSlots();
            _slotsGenerated = true;
        }

        _view.ToggleSaveSlots(true);
    }

    private void OnSettingsRequested()
    {
        Debug.Log("UI Intent: Settings Clicked. Opening Modal...");
        _uiEvents.OnRequestOpen?.Invoke(ScreenType.SettingsModal, null);
    }

    private void OnExitRequested()
    {
        Debug.Log("UI Intent: Exit Clicked. Closing Application.");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnDestroy()
    {
        if (_view != null)
        {
            _view.OnPlayClicked -= OnPlayRequested;
            _view.OnSettingsClicked -= OnSettingsRequested;
            _view.OnExitClicked -= OnExitRequested;
            _view.OnSaveSlotSelected -= HandleSlotSelected;
            _view.Dispose();
        }
    }
}