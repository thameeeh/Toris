using OutlandHaven.UIToolkit;
using System.Data;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Device;
using UnityEngine.UIElements;

public class SettingsMenuController : MonoBehaviour
{
    [SerializeField] private VisualTreeAsset _settingsTemplate;
    [SerializeField] private UIEventsSO _uiEvents;

    private SettingsMenuView _view;
    private MainMenuUIManager _uiManager;
    private InputSystem_Actions _input;

    private void Awake()
    {
        _uiManager = FindFirstObjectByType<MainMenuUIManager>();
        _input = new InputSystem_Actions();
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
        // Only close if the view is actually showing (UI Manager handles this logic usually, 
        // but we emit the intent)
        OnCloseRequested();
    }

    private void Start()
    {
        if (_settingsTemplate == null || _uiManager == null) return;

        TemplateContainer settingsInstance = _settingsTemplate.Instantiate();

        // RULE 3.5 FIX: Force the runtime wrapper to be a full-screen absolute overlay
        settingsInstance.style.position = Position.Absolute;
        settingsInstance.style.top = 0;
        settingsInstance.style.bottom = 0;
        settingsInstance.style.left = 0;
        settingsInstance.style.right = 0;

        settingsInstance.pickingMode = PickingMode.Ignore;

        _view = new SettingsMenuView(settingsInstance, _uiEvents);
        _view.Initialize();

        // Listen for the close button click
        _view.OnCloseClicked += OnCloseRequested;

        // Listen for slider changes
        _view.OnMasterVolumeChanged += (val) => Debug.Log($"[Settings] Master Volume changed to {val:F2}");
        _view.OnMusicVolumeChanged += (val) => Debug.Log($"[Settings] Music Volume changed to {val:F2}");
        _view.OnSFXVolumeChanged += (val) => Debug.Log($"[Settings] SFX Volume changed to {val:F2}");

        // Register it (it will automatically hide on start)
        _uiManager.RegisterView(_view);
    }

    private void OnCloseRequested()
    {
        // Tell the UIManager to close this specific screen
        _uiEvents.OnRequestClose?.Invoke(ScreenType.SettingsModal);
    }

    private void OnDestroy()
    {
        if (_view != null)
        {
            _view.OnCloseClicked -= OnCloseRequested;
            _view.Dispose();
        }
    }
}