using System.Collections.Generic;
using OutlandHaven.UIToolkit;
using UnityEngine;
using UnityEngine.UIElements;

public class SettingsMenuController : MonoBehaviour
{
    private const int DisplayConfirmationTimeoutSeconds = 10;
    private const string DisplayConfirmationMessageFormat = "Keep these display settings?\nReverting in {0} seconds.";

    [SerializeField] private VisualTreeAsset _settingsTemplate;
    [SerializeField] private UIEventsSO _uiEvents;

    private SettingsMenuView _view;
    private MainMenuUIManager _mainMenuUiManager;
    private UIManager _gameUiManager;
    private InputSystem_Actions _input;
    private readonly List<GameDisplayResolution> _availableResolutions = new List<GameDisplayResolution>();
    private readonly List<FullScreenMode> _availableWindowModes = new List<FullScreenMode>
    {
        FullScreenMode.FullScreenWindow,
        FullScreenMode.Windowed
    };

    private GameDisplaySettingsSnapshot _savedDisplaySettings;
    private GameDisplaySettingsSnapshot _pendingDisplaySettings;
    private GameDisplaySettingsSnapshot _previousDisplaySettings;
    private float _displayConfirmationDeadline;
    private bool _ownsCancelInput;
    private bool _awaitingDisplayConfirmation;

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
        InitializeDisplaySettings();

        _view.OnMasterVolumeChanged += HandleMasterVolumeChanged;
        _view.OnMusicVolumeChanged += HandleMusicVolumeChanged;
        _view.OnSFXVolumeChanged += HandleSfxVolumeChanged;
        _view.OnResolutionSelected += HandleResolutionSelected;
        _view.OnWindowModeSelected += HandleWindowModeSelected;
        _view.OnApplyDisplayClicked += HandleApplyDisplayClicked;
        _view.OnKeepDisplayClicked += HandleKeepDisplayClicked;
        _view.OnRevertDisplayClicked += HandleRevertDisplayClicked;
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

        if (_uiEvents != null)
        {
            _uiEvents.OnScreenClose += HandleScreenClosed;
        }
    }

    private void Update()
    {
        if (!_awaitingDisplayConfirmation)
            return;

        float remainingSeconds = _displayConfirmationDeadline - Time.unscaledTime;
        if (remainingSeconds <= 0f)
        {
            RevertPendingDisplayChanges();
            return;
        }

        _view.SetDisplayConfirmationMessage(CreateDisplayConfirmationMessage(remainingSeconds));
    }

    private void OnCloseRequested()
    {
        // Tell the UIManager to close this specific screen. Persistence is finalized
        // when the view actually closes so Escape and close-button behavior match.
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

    private void InitializeDisplaySettings()
    {
        GameDisplaySettings.Load();

        _availableResolutions.Clear();
        _availableResolutions.AddRange(GameDisplaySettings.GetAvailableResolutions());

        _savedDisplaySettings = GameDisplaySettings.SavedSettings;
        _pendingDisplaySettings = _savedDisplaySettings;
        RefreshDisplayControls();
        _view.HideDisplayConfirmation();
        RefreshDisplayControlInteractivity();
        _view.SetDisplayApplyEnabled(false);
    }

    private void HandleResolutionSelected(int selectedIndex)
    {
        if (_awaitingDisplayConfirmation || selectedIndex < 0 || selectedIndex >= _availableResolutions.Count)
            return;

        GameDisplayResolution resolution = _availableResolutions[selectedIndex];
        _pendingDisplaySettings = new GameDisplaySettingsSnapshot(
            resolution.Width,
            resolution.Height,
            _pendingDisplaySettings.WindowMode);

        RefreshDisplayApplyState();
    }

    private void HandleWindowModeSelected(int selectedIndex)
    {
        if (_awaitingDisplayConfirmation || selectedIndex < 0 || selectedIndex >= _availableWindowModes.Count)
            return;

        _pendingDisplaySettings = new GameDisplaySettingsSnapshot(
            _pendingDisplaySettings.Width,
            _pendingDisplaySettings.Height,
            _availableWindowModes[selectedIndex]);

        RefreshDisplayControls();
        RefreshDisplayControlInteractivity();
        RefreshDisplayApplyState();
    }

    private void HandleApplyDisplayClicked()
    {
        if (_awaitingDisplayConfirmation || _pendingDisplaySettings.Equals(_savedDisplaySettings))
            return;

        _previousDisplaySettings = GameDisplaySettings.CurrentSettings;
        GameDisplaySettings.Apply(_pendingDisplaySettings);

        _awaitingDisplayConfirmation = true;
        _displayConfirmationDeadline = Time.unscaledTime + DisplayConfirmationTimeoutSeconds;
        RefreshDisplayControlInteractivity();
        _view.SetDisplayApplyEnabled(false);
        _view.ShowDisplayConfirmation(CreateDisplayConfirmationMessage(DisplayConfirmationTimeoutSeconds));
    }

    private void HandleKeepDisplayClicked()
    {
        if (!_awaitingDisplayConfirmation)
            return;

        _savedDisplaySettings = _pendingDisplaySettings;
        GameDisplaySettings.Save(_savedDisplaySettings);
        EndDisplayConfirmation();
        RefreshDisplayControls();
        RefreshDisplayApplyState();
    }

    private void HandleRevertDisplayClicked()
    {
        if (_awaitingDisplayConfirmation)
        {
            RevertPendingDisplayChanges();
        }
    }

    private void HandleScreenClosed(ScreenType screenType)
    {
        if (screenType != ScreenType.SettingsModal)
            return;

        if (_awaitingDisplayConfirmation)
        {
            RevertPendingDisplayChanges();
        }
        else
        {
            ResetPendingDisplaySelection();
        }

        AudioVolumeSettings.Save();
        LootMagnetSettings.Save();
    }

    private void RevertPendingDisplayChanges()
    {
        GameDisplaySettings.Apply(_previousDisplaySettings);
        _pendingDisplaySettings = _savedDisplaySettings;
        EndDisplayConfirmation();
        RefreshDisplayControls();
        RefreshDisplayApplyState();
    }

    private void ResetPendingDisplaySelection()
    {
        _pendingDisplaySettings = _savedDisplaySettings;
        _view.HideDisplayConfirmation();
        RefreshDisplayControls();
        RefreshDisplayControlInteractivity();
        RefreshDisplayApplyState();
    }

    private void EndDisplayConfirmation()
    {
        _awaitingDisplayConfirmation = false;
        _view.HideDisplayConfirmation();
        RefreshDisplayControlInteractivity();
    }

    private void RefreshDisplayControls()
    {
        _view.SetResolutionOptions(BuildResolutionLabels(), FindResolutionIndex(_pendingDisplaySettings.Resolution));
        _view.SetWindowModeOptions(BuildWindowModeLabels(), FindWindowModeIndex(_pendingDisplaySettings.WindowMode));
        RefreshDisplayControlInteractivity();
    }

    private void RefreshDisplayApplyState()
    {
        _view.SetDisplayApplyEnabled(!_awaitingDisplayConfirmation && !_pendingDisplaySettings.Equals(_savedDisplaySettings));
    }

    private void RefreshDisplayControlInteractivity()
    {
        bool controlsEnabled = !_awaitingDisplayConfirmation;
        _view.SetWindowModeControlEnabled(controlsEnabled);
        _view.SetResolutionControlEnabled(controlsEnabled && _pendingDisplaySettings.WindowMode == FullScreenMode.Windowed);
    }

    private List<string> BuildResolutionLabels()
    {
        List<string> labels = new List<string>(_availableResolutions.Count);
        for (int i = 0; i < _availableResolutions.Count; i++)
        {
            labels.Add(_availableResolutions[i].ToString());
        }

        return labels;
    }

    private List<string> BuildWindowModeLabels()
    {
        List<string> labels = new List<string>(_availableWindowModes.Count);
        for (int i = 0; i < _availableWindowModes.Count; i++)
        {
            labels.Add(GetWindowModeLabel(_availableWindowModes[i]));
        }

        return labels;
    }

    private int FindResolutionIndex(GameDisplayResolution resolution)
    {
        for (int i = 0; i < _availableResolutions.Count; i++)
        {
            if (_availableResolutions[i].Equals(resolution))
            {
                return i;
            }
        }

        return 0;
    }

    private int FindWindowModeIndex(FullScreenMode windowMode)
    {
        FullScreenMode supportedWindowMode = GameDisplaySettings.ResolveSupportedWindowMode(windowMode);
        for (int i = 0; i < _availableWindowModes.Count; i++)
        {
            if (_availableWindowModes[i] == supportedWindowMode)
            {
                return i;
            }
        }

        return 0;
    }

    private static string GetWindowModeLabel(FullScreenMode windowMode)
    {
        return GameDisplaySettings.ResolveSupportedWindowMode(windowMode) == FullScreenMode.Windowed
            ? "Windowed"
            : "Fullscreen";
    }

    private static string CreateDisplayConfirmationMessage(float remainingSeconds)
    {
        return string.Format(
            DisplayConfirmationMessageFormat,
            Mathf.CeilToInt(Mathf.Max(0f, remainingSeconds)));
    }

    private void OnDestroy()
    {
        if (_awaitingDisplayConfirmation)
        {
            RevertPendingDisplayChanges();
        }

        if (_uiEvents != null) _uiEvents.OnScreenClose -= HandleScreenClosed;

        if (_view != null)
        {
            _view.OnCloseClicked -= OnCloseRequested;
            _view.OnMasterVolumeChanged -= HandleMasterVolumeChanged;
            _view.OnMusicVolumeChanged -= HandleMusicVolumeChanged;
            _view.OnSFXVolumeChanged -= HandleSfxVolumeChanged;
            _view.OnResolutionSelected -= HandleResolutionSelected;
            _view.OnWindowModeSelected -= HandleWindowModeSelected;
            _view.OnApplyDisplayClicked -= HandleApplyDisplayClicked;
            _view.OnKeepDisplayClicked -= HandleKeepDisplayClicked;
            _view.OnRevertDisplayClicked -= HandleRevertDisplayClicked;
            _view.OnLootMagnetToggled -= HandleLootMagnetToggled;
            _view.Dispose();
        }
    }
}
