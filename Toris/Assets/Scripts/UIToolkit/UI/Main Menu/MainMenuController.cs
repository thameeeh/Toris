using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using OutlandHaven.UIToolkit;
using OutlandHaven.SaveSystem;

public class MainMenuController : MonoBehaviour
{
    private const string MainAreaSceneName = "MainArea";
    private const string PrologueSceneName = "Prologue";

    [Header("Templates")]
    [SerializeField] private VisualTreeAsset _mainMenuTemplate;
    [SerializeField] private VisualTreeAsset _saveSlotTemplate; // Added for the Slot Cards

    [Header("Dependencies")]
    [SerializeField] private UIEventsSO _uiEvents;
    [SerializeField] private SaveManager _saveManager;
    [SerializeField] private MainMenuSong _mainMenuSong;

    [Header("Music")]
    [SerializeField, Min(0f)] private float _startGameMusicFadeOutSeconds = 1f;

    [Header("New Game")]
    [SerializeField] private string _newGameSceneName = PrologueSceneName;

    private MainMenuView _view;
    private MainMenuUIManager _uiManager;
    private InputSystem_Actions _input;

    private bool _slotsGenerated = false;
    private bool _isShowingSlots = false;
    private bool _isStartingGame = false;

    private void Awake()
    {
        _uiManager = FindFirstObjectByType<MainMenuUIManager>();
        _input = new InputSystem_Actions();
        
        // Ensure we have a SaveManager if not manually assigned
        if (_saveManager == null)
        {
            _saveManager = FindFirstObjectByType<SaveManager>();
        }

        if (_mainMenuSong == null)
        {
            _mainMenuSong = MainMenuSong.Instance != null
                ? MainMenuSong.Instance
                : FindFirstObjectByType<MainMenuSong>();
        }
    }

    private void OnEnable()
    {
        _input?.UI.Enable();
        if (_input != null)
        {
            _input.UI.Cancel.performed += OnCancelPerformed;
        }
    }

    private void OnDisable()
    {
        _input?.UI.Disable();
        if (_input != null)
        {
            _input.UI.Cancel.performed -= OnCancelPerformed;
        }
    }

    private void OnCancelPerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (_isShowingSlots)
        {
            OnCloseSlotsRequested();
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
        _view.OnCloseSlotsClicked += OnCloseSlotsRequested;
        _view.OnSaveSlotSelected += HandleSlotSelected;
        _view.OnSaveSlotDeleteRequested += HandleSlotDelete;

        // 4. Register the view with the Manager
        _uiManager.RegisterView(_view);
    }

    private void PopulateSaveSlotsFromFiles()
    {
        if (_saveManager == null) return;

        List<SaveSlotData> slotDataList = new List<SaveSlotData>();

        // We currently support 3 slots (Slot1, Slot2, Slot3)
        for (int i = 0; i < 3; i++)
        {
            SaveSlotIndex enumIndex = (SaveSlotIndex)i;
            SaveMetadata metadata = _saveManager.PeekSaveMetadata(enumIndex);

            if (metadata != null)
            {
                slotDataList.Add(new SaveSlotData
                {
                    SlotIndex = i + 1,
                    Level = metadata.Level,
                    Gold = metadata.Gold,
                    Timestamp = metadata.SaveTime
                });
            }
            else
            {
                slotDataList.Add(new SaveSlotData
                {
                    SlotIndex = i + 1,
                    Level = 0,
                    Gold = 0,
                    Timestamp = "Empty Slot"
                });
            }
        }

        _view.PopulateSaveSlots(slotDataList);
    }

    // --- Intent Handlers ---

    private void OnCloseSlotsRequested()
    {
        _isShowingSlots = false;
        _view.ToggleSaveSlots(false);
    }

    private void HandleSlotDelete(int slotIndex)
    {
        if (_saveManager == null) return;

        SaveSlotIndex enumIndex = (SaveSlotIndex)(slotIndex - 1);
        
        ConfirmationPayload payload = new ConfirmationPayload(
            "DELETE SAVE",
            $"Are you sure you want to permanently delete Slot {slotIndex}?\nThis action cannot be undone.",
            () => {
                Debug.Log($"UI Intent: Delete Save Slot {slotIndex} ({enumIndex}).");
                _saveManager.DeleteSave(enumIndex);
                PopulateSaveSlotsFromFiles();
            }
        );

        _uiEvents.OnRequestOpen?.Invoke(ScreenType.ConfirmationModal, payload);
    }

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

        // Store the active slot in the session
        _saveManager.ActiveSession.ActiveSaveSlot = enumIndex;
        _saveManager.ActiveSession.AllowAutoSaveForSlot(enumIndex);

        // 1. Load the data
        GameSaveData loadedData = _saveManager.LoadGameData(enumIndex);

        if (loadedData != null)
        {
            // 2. Apply to session
            _saveManager.ActiveSession.ImportFromSaveData(loadedData, _saveManager.MasterItemDatabase);

            // 3. Transition Scene
            StartGameSceneLoad(ResolveLoadedGameSceneName(loadedData));
        }
        else
        {
            Debug.Log($"[MainMenuController] Slot {slotIndex} is empty. Initializing New Game sequence...");
            PromptTutorialChoiceForNewGame(slotIndex, enumIndex);
        }
    }

    private void PromptTutorialChoiceForNewGame(int slotIndex, SaveSlotIndex enumIndex)
    {
        ConfirmationPayload payload = new ConfirmationPayload(
            "TUTORIAL",
            $"Would you like tutorial guidance for Slot {slotIndex}?",
            () => BeginNewGame(enumIndex, tutorialsEnabled: true),
            () => BeginNewGame(enumIndex, tutorialsEnabled: false),
            "Yes",
            "No"
        );

        _uiEvents.OnRequestOpen?.Invoke(ScreenType.ConfirmationModal, payload);
    }

    private void BeginNewGame(SaveSlotIndex enumIndex, bool tutorialsEnabled)
    {
        if (_saveManager == null || _saveManager.ActiveSession == null)
            return;

        _saveManager.ActiveSession.ActiveSaveSlot = enumIndex;
        _saveManager.ActiveSession.AllowAutoSaveForSlot(enumIndex);
        _saveManager.ActiveSession.PrepareNewGame(tutorialsEnabled);
        StartGameSceneLoad(ResolveNewGameSceneName());
    }

    private string ResolveNewGameSceneName()
    {
        return string.IsNullOrWhiteSpace(_newGameSceneName)
            ? PrologueSceneName
            : _newGameSceneName.Trim();
    }

    private static string ResolveLoadedGameSceneName(GameSaveData loadedData)
    {
        if (loadedData == null)
            return MainAreaSceneName;

        if (string.IsNullOrWhiteSpace(loadedData.CurrentSceneName))
            return MainAreaSceneName;

        // Prologue completion is a progression gate, not a scene-export concern.
        // Handoff saves still record Prologue as active, so only those redirect.
        if (loadedData.PrologueCompleted
            && string.Equals(loadedData.CurrentSceneName.Trim(), PrologueSceneName, System.StringComparison.OrdinalIgnoreCase))
        {
            return MainAreaSceneName;
        }

        return loadedData.CurrentSceneName.Trim();
    }

    private void StartGameSceneLoad(string sceneName)
    {
        if (_isStartingGame)
            return;

        _isStartingGame = true;
        StartCoroutine(StartGameSceneLoadRoutine(sceneName));
    }

    private IEnumerator StartGameSceneLoadRoutine(string sceneName)
    {
        float fadeWaitSeconds = 0f;

        if (_mainMenuSong != null)
        {
            // Music-only handoff: fade the bespoke menu stem arrangement before the gameplay scene load starts.
            fadeWaitSeconds = _mainMenuSong.FadeOutAndStop(_startGameMusicFadeOutSeconds);
        }

        float waitUntil = Time.realtimeSinceStartup + fadeWaitSeconds;
        while (Time.realtimeSinceStartup < waitUntil)
        {
            yield return null;
        }

        if (SceneTransitionService.Instance != null)
        {
            SceneTransitionService.Instance.LoadScene(sceneName);
            yield break;
        }

        SceneManager.LoadScene(sceneName);
    }

    private void OnPlayRequested()
    {
        Debug.Log("UI Intent: Play Clicked. (Slots are already visible on the right).");

        if (!_slotsGenerated)
        {
            PopulateSaveSlotsFromFiles();
            _slotsGenerated = true;
        }

        _isShowingSlots = true;
        _view.ToggleSaveSlots(true);
    }

    private void OnSettingsRequested()
    {
        Debug.Log("UI Intent: Settings Clicked. Opening Modal...");
        _uiEvents.OnRequestOpen?.Invoke(ScreenType.SettingsModal, null);
    }

    private void OnExitRequested()
    {
        ConfirmationPayload payload = new ConfirmationPayload(
            "EXIT GAME",
            "Are you sure you want to quit to desktop?",
            () => {
                Debug.Log("UI Intent: Exit Confirmed. Closing Application.");
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        );

        _uiEvents.OnRequestOpen?.Invoke(ScreenType.ConfirmationModal, payload);
    }

    private void OnDestroy()
    {
        if (_view != null)
        {
            _view.OnPlayClicked -= OnPlayRequested;
            _view.OnSettingsClicked -= OnSettingsRequested;
            _view.OnExitClicked -= OnExitRequested;
            _view.OnSaveSlotSelected -= HandleSlotSelected;
            _view.OnSaveSlotDeleteRequested -= HandleSlotDelete;
            _view.Dispose();
        }
    }
}
