using UnityEngine;
using UnityEngine.UIElements;
using OutlandHaven.UIToolkit;

public class MainMenuController : MonoBehaviour
{
    [Header("Templates")]
    [SerializeField] private VisualTreeAsset _mainMenuTemplate;

    [Header("Dependencies")]
    [SerializeField] private UIEventsSO _uiEvents;
    // (Future dependency: SaveManager/TransitionService will go here)

    private MainMenuView _view;
    private MainMenuUIManager _uiManager;

    private void Awake()
    {
        // Find the unified UIManager just like InventoryScreenController does[cite: 12]
        _uiManager = FindFirstObjectByType<MainMenuUIManager>();
    }

    private void OnValidate()
    {
        if (_uiEvents == null)
        {
            Debug.LogError($" <color=red>{name}</color> missing UI Events SO", this);
        }
    }

    private void Start()
    {
        if (_mainMenuTemplate == null || _uiManager == null) return;

        // 1. Instantiate the UXML template[cite: 7]
        TemplateContainer menuInstance = _mainMenuTemplate.Instantiate();

        // 2. Strict CSS Rule Validation: Apply flexGrow to the spawned instance[cite: 6]
        menuInstance.style.flexGrow = 1;

        // 3. Construct and Initialize the pure C# View, passing the UIEventsSO[cite: 10]
        _view = new MainMenuView(menuInstance, _uiEvents);
        _view.Initialize();

        // 4. Subscribe to the View's pure C# actions
        _view.OnPlayClicked += OnPlayRequested;
        _view.OnSettingsClicked += OnSettingsRequested;
        _view.OnExitClicked += OnExitRequested;

        // 5. CORRECTED: Delegate DOM appending and visibility to the UIManager[cite: 6]
        _uiManager.RegisterView(_view);

        // Note: The UIManager handles calling Show() when appropriate based on its rules.
    }

    private void OnDestroy()
    {
        if (_view != null)
        {
            _view.OnPlayClicked -= OnPlayRequested;
            _view.OnSettingsClicked -= OnSettingsRequested;
            _view.OnExitClicked -= OnExitRequested;
            _view.Dispose();
        }
    }

    // --- Intent Handlers ---

    private void OnPlayRequested()
    {
        Debug.Log("UI Intent: Play Clicked.");
        // Future: Transition scene, tell SaveManager to load selected slot.
    }

    private void OnSettingsRequested()
    {
        Debug.Log("UI Intent: Settings Clicked. Opening Modal...");
        // Use your event system to open a modal window[cite: 9]
        _uiEvents.OnRequestOpen?.Invoke(ScreenType.SettingsModal, null); // Assuming you add SettingsModal to ScreenType
    }

    private void OnExitRequested()
    {
        Debug.Log("UI Intent: Exit Clicked. Closing Application.");
        Application.Quit();
    }
}