using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using OutlandHaven.UIToolkit;

public class MainMenuController : MonoBehaviour
{
    [Header("Templates")]
    [SerializeField] private VisualTreeAsset _mainMenuTemplate;
    [SerializeField] private VisualTreeAsset _saveSlotTemplate; // Added for the Slot Cards

    [Header("Dependencies")]
    [SerializeField] private UIEventsSO _uiEvents;

    private MainMenuView _view;
    private MainMenuUIManager _uiManager;

    private bool _slotsGenerated = false;

    private void Awake()
    {
        _uiManager = FindFirstObjectByType<MainMenuUIManager>();
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
        Debug.Log($"UI Intent: Selected Save Slot {slotIndex}. Commencing load sequence...");
        // Future Integration: 
        // 1. Pass slotIndex to SaveManager so it knows which file to read.
        // 2. Trigger SceneTransitionService to load the gameplay scene.
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