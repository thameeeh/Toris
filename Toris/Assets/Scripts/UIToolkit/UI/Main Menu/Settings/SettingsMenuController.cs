using OutlandHaven.UIToolkit;
using UnityEngine;
using UnityEngine.UIElements;

public class SettingsMenuController : MonoBehaviour
{
    [SerializeField] private VisualTreeAsset _settingsTemplate;
    [SerializeField] private UIEventsSO _uiEvents;

    private SettingsMenuView _view;
    private MainMenuUIManager _mainMenuUiManager;
    private UIManager _gameUiManager;
    private InputSystem_Actions _input;
    private bool _ownsCancelInput;

    private void Awake()
    {
        _mainMenuUiManager = FindFirstObjectByType<MainMenuUIManager>();
        if (_mainMenuUiManager == null)
        {
            _gameUiManager = FindFirstObjectByType<UIManager>();
        }
        else
        {
            _ownsCancelInput = true;
        }

        _input = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        if (!_ownsCancelInput)
            return;

        // The main menu has no gameplay InputManager, so Settings owns Escape there.
        _input?.UI.Enable();
        if (_input != null)
        {
            _input.UI.Cancel.performed += OnCancelPerformed;
        }
    }

    private void OnDisable()
    {
        if (!_ownsCancelInput)
            return;

        _input?.UI.Disable();
        if (_input != null)
        {
            _input.UI.Cancel.performed -= OnCancelPerformed;
        }
    }

    private void OnCancelPerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (_view == null || _view.IsHidden)
            return;

        OnCloseRequested();
    }

    private void Start()
    {
        if (_settingsTemplate == null || (_mainMenuUiManager == null && _gameUiManager == null)) return;

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

        _view.SetVolumeValues(
            AudioVolumeSettings.MasterVolume,
            AudioVolumeSettings.MusicVolume,
            AudioVolumeSettings.SfxVolume);

        _view.SetLootMagnetValue(LootMagnetSettings.LootMagnetEnabled);

        _view.OnMasterVolumeChanged += HandleMasterVolumeChanged;
        _view.OnMusicVolumeChanged += HandleMusicVolumeChanged;
        _view.OnSFXVolumeChanged += HandleSfxVolumeChanged;
        _view.OnLootMagnetToggled += HandleLootMagnetToggled;

        if (_mainMenuUiManager != null)
        {
            _mainMenuUiManager.RegisterView(_view);
        }
        else
        {
            // Settings is shared by main menu and gameplay; gameplay mounts it as a modal overlay.
            _gameUiManager.RegisterView(_view, ScreenZone.Modal);
        }
    }

    private void OnCloseRequested()
    {
        // Tell the UIManager to close this specific screen
        AudioVolumeSettings.Save();
        LootMagnetSettings.Save();
        _uiEvents.OnRequestClose?.Invoke(ScreenType.SettingsModal);
    }

    private void HandleMasterVolumeChanged(float value)
    {
        // Audio settings only: sliders change saved mix values, not UI state or gameplay.
        AudioVolumeSettings.SetMasterVolume(value);
    }

    private void HandleMusicVolumeChanged(float value)
    {
        // Audio settings only: sliders change saved mix values, not UI state or gameplay.
        AudioVolumeSettings.SetMusicVolume(value);
    }

    private void HandleSfxVolumeChanged(float value)
    {
        // Audio settings only: sliders change saved mix values, not UI state or gameplay.
        AudioVolumeSettings.SetSfxVolume(value);
    }

    private void HandleLootMagnetToggled(bool value)
    {
        LootMagnetSettings.SetLootMagnetEnabled(value);
    }

    private void OnDestroy()
    {
        if (_view != null)
        {
            _view.OnCloseClicked -= OnCloseRequested;
            _view.OnMasterVolumeChanged -= HandleMasterVolumeChanged;
            _view.OnMusicVolumeChanged -= HandleMusicVolumeChanged;
            _view.OnSFXVolumeChanged -= HandleSfxVolumeChanged;
            _view.OnLootMagnetToggled -= HandleLootMagnetToggled;
            _view.Dispose();
        }
    }
}
