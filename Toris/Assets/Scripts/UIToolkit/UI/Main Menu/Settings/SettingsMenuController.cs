using System.Collections;
using System.Collections.Generic;
using OutlandHaven.UIToolkit;
using UnityEngine;
using UnityEngine.InputSystem;
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
    private InputSystem_Actions _bindingActions;
    private Coroutine _displayApplyRoutine;
    private InputActionRebindingExtensions.RebindingOperation _rebindingOperation;
    private InputAction _rebindingAction;
    private readonly List<GameDisplayOption> _availableDisplays = new List<GameDisplayOption>();
    private readonly List<GameDisplayResolution> _availableResolutions = new List<GameDisplayResolution>();
    private readonly List<InputBindingDisplayEntry> _controlBindingEntries = new List<InputBindingDisplayEntry>();
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
    private bool _displayApplyInProgress;
    private bool _rebindingActionWasEnabled;
    private int _rebindingBindingIndex = -1;
    private string _previousBindingOverridePath;
    private string _activeControlRebindId;

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
        // Settings rebinding hook: Settings owns this input instance in the main menu.
        InputBindingSettings.ApplyTo(_input);
        _bindingActions = new InputSystem_Actions();
        InputBindingSettings.ApplyTo(_bindingActions);
    }

    private void OnEnable()
    {
        InputBindingSettings.OnBindingsChanged += HandleInputBindingsChanged;

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
        InputBindingSettings.OnBindingsChanged -= HandleInputBindingsChanged;
        CancelActiveControlRebind();

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

        if (_rebindingOperation != null)
        {
            CancelActiveControlRebind();
            return;
        }

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
        InitializeControlSettings();

        _view.OnMasterVolumeChanged += HandleMasterVolumeChanged;
        _view.OnMusicVolumeChanged += HandleMusicVolumeChanged;
        _view.OnSFXVolumeChanged += HandleSfxVolumeChanged;
        _view.OnDisplaySelected += HandleDisplaySelected;
        _view.OnResolutionSelected += HandleResolutionSelected;
        _view.OnWindowModeSelected += HandleWindowModeSelected;
        _view.OnApplyDisplayClicked += HandleApplyDisplayClicked;
        _view.OnKeepDisplayClicked += HandleKeepDisplayClicked;
        _view.OnRevertDisplayClicked += HandleRevertDisplayClicked;
        _view.OnLootMagnetToggled += HandleLootMagnetToggled;
        _view.OnRebindControlRequested += HandleRebindControlRequested;
        _view.OnResetControlRequested += HandleResetControlRequested;
        _view.OnResetAllControlsRequested += HandleResetAllControlsRequested;

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

    private void InitializeControlSettings()
    {
        RefreshControlBindings();
        _view.SetControlRebindStatus(string.Empty);
    }

    private void InitializeDisplaySettings()
    {
        GameDisplaySettings.Load();

        _availableDisplays.Clear();
        _availableDisplays.AddRange(GameDisplaySettings.GetAvailableDisplays());

        _savedDisplaySettings = GameDisplaySettings.SavedSettings;
        _pendingDisplaySettings = _savedDisplaySettings;
        RefreshAvailableResolutions();
        RefreshDisplayControls();
        _view.HideDisplayConfirmation();
        RefreshDisplayControlInteractivity();
        _view.SetDisplayApplyEnabled(false);
    }

    private void HandleDisplaySelected(int selectedIndex)
    {
        if (_awaitingDisplayConfirmation || _displayApplyInProgress || selectedIndex < 0 || selectedIndex >= _availableDisplays.Count)
            return;

        GameDisplayOption displayOption = _availableDisplays[selectedIndex];
        _pendingDisplaySettings = GameDisplaySettings.Normalize(new GameDisplaySettingsSnapshot(
            _pendingDisplaySettings.Width,
            _pendingDisplaySettings.Height,
            _pendingDisplaySettings.WindowMode,
            displayOption.Index));

        RefreshAvailableResolutions();
        RefreshDisplayControls();
        RefreshDisplayApplyState();
    }

    private void HandleResolutionSelected(int selectedIndex)
    {
        if (_awaitingDisplayConfirmation || _displayApplyInProgress || selectedIndex < 0 || selectedIndex >= _availableResolutions.Count)
            return;

        GameDisplayResolution resolution = _availableResolutions[selectedIndex];
        _pendingDisplaySettings = new GameDisplaySettingsSnapshot(
            resolution.Width,
            resolution.Height,
            _pendingDisplaySettings.WindowMode,
            _pendingDisplaySettings.DisplayIndex);

        RefreshDisplayApplyState();
    }

    private void HandleWindowModeSelected(int selectedIndex)
    {
        if (_awaitingDisplayConfirmation || _displayApplyInProgress || selectedIndex < 0 || selectedIndex >= _availableWindowModes.Count)
            return;

        _pendingDisplaySettings = new GameDisplaySettingsSnapshot(
            _pendingDisplaySettings.Width,
            _pendingDisplaySettings.Height,
            _availableWindowModes[selectedIndex],
            _pendingDisplaySettings.DisplayIndex);

        RefreshDisplayControls();
        RefreshDisplayControlInteractivity();
        RefreshDisplayApplyState();
    }

    private void HandleApplyDisplayClicked()
    {
        if (_awaitingDisplayConfirmation || _displayApplyInProgress || _pendingDisplaySettings.Equals(_savedDisplaySettings))
            return;

        _displayApplyRoutine = StartCoroutine(ApplyDisplayChangesRoutine());
    }

    private IEnumerator ApplyDisplayChangesRoutine()
    {
        _displayApplyInProgress = true;
        _previousDisplaySettings = GameDisplaySettings.CurrentSettings;
        RefreshDisplayControlInteractivity();
        _view.SetDisplayApplyEnabled(false);

        AsyncOperation moveOperation = GameDisplaySettings.MoveMainWindowToDisplay(_pendingDisplaySettings);
        while (moveOperation != null && !moveOperation.isDone)
        {
            yield return null;
        }

        GameDisplaySettings.ApplyResolutionAndMode(_pendingDisplaySettings);

        _displayApplyInProgress = false;
        _displayApplyRoutine = null;
        _awaitingDisplayConfirmation = true;
        _displayConfirmationDeadline = Time.unscaledTime + DisplayConfirmationTimeoutSeconds;
        RefreshDisplayControlInteractivity();
        _view.ShowDisplayConfirmation(CreateDisplayConfirmationMessage(DisplayConfirmationTimeoutSeconds));
    }

    private void HandleKeepDisplayClicked()
    {
        if (!_awaitingDisplayConfirmation)
            return;

        GameDisplaySettings.Save(_pendingDisplaySettings);
        _savedDisplaySettings = GameDisplaySettings.SavedSettings;
        _pendingDisplaySettings = _savedDisplaySettings;
        RefreshAvailableResolutions();
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

        CancelActiveControlRebind();

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
        if (_displayApplyRoutine != null)
        {
            StopCoroutine(_displayApplyRoutine);
            _displayApplyRoutine = null;
            _displayApplyInProgress = false;
        }

        GameDisplaySettings.Apply(_previousDisplaySettings);
        _pendingDisplaySettings = _savedDisplaySettings;
        RefreshAvailableResolutions();
        EndDisplayConfirmation();
        RefreshDisplayControls();
        RefreshDisplayApplyState();
    }

    private void ResetPendingDisplaySelection()
    {
        _pendingDisplaySettings = _savedDisplaySettings;
        _view.HideDisplayConfirmation();
        RefreshAvailableResolutions();
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
        _view.SetDisplayOptions(BuildDisplayLabels(), FindDisplayIndex(_pendingDisplaySettings.DisplayIndex));
        _view.SetResolutionOptions(BuildResolutionLabels(), FindResolutionIndex(_pendingDisplaySettings.Resolution));
        _view.SetWindowModeOptions(BuildWindowModeLabels(), FindWindowModeIndex(_pendingDisplaySettings.WindowMode));
        RefreshDisplayControlInteractivity();
    }

    private void RefreshDisplayApplyState()
    {
        _view.SetDisplayApplyEnabled(!_awaitingDisplayConfirmation
            && !_displayApplyInProgress
            && !_pendingDisplaySettings.Equals(_savedDisplaySettings));
    }

    private void RefreshDisplayControlInteractivity()
    {
        bool controlsEnabled = !_awaitingDisplayConfirmation && !_displayApplyInProgress;
        _view.SetDisplayControlEnabled(controlsEnabled);
        _view.SetWindowModeControlEnabled(controlsEnabled);
        _view.SetResolutionControlEnabled(controlsEnabled && _pendingDisplaySettings.WindowMode == FullScreenMode.Windowed);
    }

    private void RefreshAvailableResolutions()
    {
        _availableResolutions.Clear();
        _availableResolutions.AddRange(GameDisplaySettings.GetAvailableResolutions(_pendingDisplaySettings.DisplayIndex));
    }

    private List<string> BuildDisplayLabels()
    {
        List<string> labels = new List<string>(_availableDisplays.Count);
        for (int i = 0; i < _availableDisplays.Count; i++)
        {
            labels.Add(_availableDisplays[i].ToString());
        }

        return labels;
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

    private int FindDisplayIndex(int displayIndex)
    {
        for (int i = 0; i < _availableDisplays.Count; i++)
        {
            if (_availableDisplays[i].Index == displayIndex)
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

    private void HandleRebindControlRequested(string entryId)
    {
        if (_rebindingOperation != null)
            return;

        if (!InputBindingSettings.TryFindDisplayEntry(_controlBindingEntries, entryId, out InputBindingDisplayEntry entry))
            return;

        InputAction action = InputBindingSettings.FindAction(_bindingActions, entry.ActionMapName, entry.ActionName);
        if (action == null || entry.BindingIndex < 0 || entry.BindingIndex >= action.bindings.Count)
            return;

        _activeControlRebindId = entry.Id;
        _rebindingAction = action;
        _rebindingBindingIndex = entry.BindingIndex;
        _previousBindingOverridePath = action.bindings[entry.BindingIndex].overridePath;
        _rebindingActionWasEnabled = action.enabled;
        if (_rebindingActionWasEnabled)
        {
            action.Disable();
        }

        _view.SetControlRebindStatus($"Listening for {entry.DisplayName}");
        RefreshControlBindings();

        _rebindingOperation = action.PerformInteractiveRebinding(entry.BindingIndex)
            .WithControlsExcluding("<Gamepad>")
            .WithControlsExcluding("<Joystick>")
            .WithControlsExcluding("<Touchscreen>")
            .WithControlsExcluding("<XRController>")
            .WithCancelingThrough("<Keyboard>/escape")
            .OnCancel(operation => CompleteControlRebind(saveChanges: false))
            .OnComplete(operation => CompleteControlRebind(saveChanges: true))
            .Start();
    }

    private void HandleResetControlRequested(string entryId)
    {
        if (_rebindingOperation != null)
            return;

        if (!InputBindingSettings.TryFindDisplayEntry(_controlBindingEntries, entryId, out InputBindingDisplayEntry entry))
            return;

        InputAction action = InputBindingSettings.FindAction(_bindingActions, entry.ActionMapName, entry.ActionName);
        if (action == null || entry.BindingIndex < 0 || entry.BindingIndex >= action.bindings.Count)
            return;

        action.RemoveBindingOverride(entry.BindingIndex);
        InputBindingSettings.SaveOverrides(_bindingActions);
    }

    private void HandleResetAllControlsRequested()
    {
        if (_rebindingOperation != null)
            return;

        _bindingActions?.asset.RemoveAllBindingOverrides();
        InputBindingSettings.SaveOverrides(_bindingActions);
    }

    private void HandleInputBindingsChanged()
    {
        InputBindingSettings.ApplyTo(_input);
        if (_rebindingOperation == null)
        {
            InputBindingSettings.ApplyTo(_bindingActions);
        }

        RefreshControlBindings();
    }

    private void CancelActiveControlRebind()
    {
        _rebindingOperation?.Cancel();
    }

    private void CompleteControlRebind(bool saveChanges)
    {
        InputActionRebindingExtensions.RebindingOperation completedOperation = _rebindingOperation;
        _rebindingOperation = null;
        completedOperation?.Dispose();

        string statusText = string.Empty;
        if (saveChanges
            && InputBindingSettings.TryFindDisplayEntry(_controlBindingEntries, _activeControlRebindId, out InputBindingDisplayEntry activeEntry)
            && InputBindingSettings.TryFindDuplicateBinding(_bindingActions, _controlBindingEntries, activeEntry, out InputBindingDisplayEntry duplicateEntry))
        {
            RestorePreviousControlBindingOverride();
            statusText = $"{activeEntry.DisplayName} already conflicts with {duplicateEntry.DisplayName}.";
            saveChanges = false;
        }

        if (_rebindingAction != null && _rebindingActionWasEnabled)
        {
            _rebindingAction.Enable();
        }

        _rebindingAction = null;
        _rebindingActionWasEnabled = false;
        _rebindingBindingIndex = -1;
        _previousBindingOverridePath = null;
        _activeControlRebindId = null;
        _view?.SetControlRebindStatus(statusText);

        if (saveChanges)
        {
            InputBindingSettings.SaveOverrides(_bindingActions);
            return;
        }

        RefreshControlBindings();
    }

    private void RefreshControlBindings()
    {
        _controlBindingEntries.Clear();
        _controlBindingEntries.AddRange(InputBindingSettings.GetDisplayEntries(_bindingActions));
        _view?.SetControlBindings(_controlBindingEntries, _activeControlRebindId);
    }

    private void RestorePreviousControlBindingOverride()
    {
        if (_rebindingAction == null
            || _rebindingBindingIndex < 0
            || _rebindingBindingIndex >= _rebindingAction.bindings.Count)
        {
            return;
        }

        if (string.IsNullOrEmpty(_previousBindingOverridePath))
        {
            _rebindingAction.RemoveBindingOverride(_rebindingBindingIndex);
            return;
        }

        _rebindingAction.ApplyBindingOverride(_rebindingBindingIndex, _previousBindingOverridePath);
    }

    private void OnDestroy()
    {
        CancelActiveControlRebind();

        bool wasDisplayApplyInProgress = _displayApplyInProgress;
        if (_displayApplyRoutine != null)
        {
            StopCoroutine(_displayApplyRoutine);
            _displayApplyRoutine = null;
            _displayApplyInProgress = false;
        }

        if (wasDisplayApplyInProgress)
        {
            GameDisplaySettings.Apply(_previousDisplaySettings);
        }

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
            _view.OnDisplaySelected -= HandleDisplaySelected;
            _view.OnResolutionSelected -= HandleResolutionSelected;
            _view.OnWindowModeSelected -= HandleWindowModeSelected;
            _view.OnApplyDisplayClicked -= HandleApplyDisplayClicked;
            _view.OnKeepDisplayClicked -= HandleKeepDisplayClicked;
            _view.OnRevertDisplayClicked -= HandleRevertDisplayClicked;
            _view.OnLootMagnetToggled -= HandleLootMagnetToggled;
            _view.OnRebindControlRequested -= HandleRebindControlRequested;
            _view.OnResetControlRequested -= HandleResetControlRequested;
            _view.OnResetAllControlsRequested -= HandleResetAllControlsRequested;
            _view.Dispose();
        }

        _bindingActions?.Dispose();
    }
}
