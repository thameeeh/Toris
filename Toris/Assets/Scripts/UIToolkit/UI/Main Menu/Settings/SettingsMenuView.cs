using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using OutlandHaven.UIToolkit;

public class SettingsMenuView : GameView
{
    public override ScreenType ID => ScreenType.SettingsModal;
    private const string ActiveTabClass = "panel-tab--active";

    private Button _closeButton;
    private VisualElement _mainTab;
    private VisualElement _controlsTab;
    private VisualElement _mainContent;
    private VisualElement _controlsContent;
    private VisualElement _keyboardControlBindingsList;
    private VisualElement _gamepadControlSection;
    private VisualElement _gamepadControlBindingsList;
    private Label _controlRebindStatus;
    private Button _resetAllControlsButton;
    private Slider _masterVolumeSlider;
    private Slider _musicVolumeSlider;
    private Slider _sfxVolumeSlider;
    private DropdownField _displayDropdown;
    private DropdownField _resolutionDropdown;
    private DropdownField _windowModeDropdown;
    private Button _applyDisplayButton;
    private VisualElement _displayConfirmationOverlay;
    private Label _displayConfirmationMessage;
    private Button _keepDisplayButton;
    private Button _revertDisplayButton;
    private Toggle _lootMagnetToggle;
    private Toggle _damageNumbersToggle;
    private Toggle _showFpsToggle;
    private readonly Dictionary<string, Button> _controlRebindButtons = new Dictionary<string, Button>();
    private readonly Dictionary<string, Button> _controlResetButtons = new Dictionary<string, Button>();

    public event Action OnCloseClicked;
    public event Action<float> OnMasterVolumeChanged;
    public event Action<float> OnMusicVolumeChanged;
    public event Action<float> OnSFXVolumeChanged;
    public event Action<int> OnDisplaySelected;
    public event Action<int> OnResolutionSelected;
    public event Action<int> OnWindowModeSelected;
    public event Action OnApplyDisplayClicked;
    public event Action OnKeepDisplayClicked;
    public event Action OnRevertDisplayClicked;
    public event Action<bool> OnLootMagnetToggled;
    public event Action<bool> OnDamageNumbersToggled;
    public event Action<bool> OnShowFpsToggled;
    public event Action<string> OnRebindControlRequested;
    public event Action<string> OnResetControlRequested;
    public event Action OnResetAllControlsRequested;

    public SettingsMenuView(VisualElement topElement, UIEventsSO uiEvents) : base(topElement, uiEvents) { }

    public override void Initialize()
    {
        base.Initialize();
        _closeButton = Root.Q<Button>("Btn_Close");
        if (_closeButton != null) _closeButton.clicked += HandleCloseClicked;

        _mainTab = Root.Q<VisualElement>("Settings_MainTab");
        if (_mainTab != null)
        {
            _mainTab.RegisterCallback<ClickEvent>(HandleMainTabClicked);
        }

        _controlsTab = Root.Q<VisualElement>("Settings_ControlsTab");
        if (_controlsTab != null)
        {
            _controlsTab.RegisterCallback<ClickEvent>(HandleControlsTabClicked);
        }

        _mainContent = Root.Q<VisualElement>("Settings_MainContent");
        _controlsContent = Root.Q<VisualElement>("Settings_ControlsContent");
        _keyboardControlBindingsList = Root.Q<VisualElement>("Controls_KeyboardBindingsList");
        _gamepadControlSection = Root.Q<VisualElement>("Controls_GamepadSection");
        _gamepadControlBindingsList = Root.Q<VisualElement>("Controls_GamepadBindingsList");
        _controlRebindStatus = Root.Q<Label>("Label_ControlRebindStatus");
        _resetAllControlsButton = Root.Q<Button>("Btn_ResetAllControls");
        if (_resetAllControlsButton != null)
        {
            _resetAllControlsButton.clicked += HandleResetAllControlsClicked;
        }

        _masterVolumeSlider = Root.Q<Slider>("Slider_MasterVolume");
        if (_masterVolumeSlider != null)
        {
            _masterVolumeSlider.RegisterValueChangedCallback(HandleMasterVolumeChanged);
        }

        _musicVolumeSlider = Root.Q<Slider>("Slider_MusicVolume");
        if (_musicVolumeSlider != null)
        {
            _musicVolumeSlider.RegisterValueChangedCallback(HandleMusicVolumeChanged);
        }

        _sfxVolumeSlider = Root.Q<Slider>("Slider_SFXVolume");
        if (_sfxVolumeSlider != null)
        {
            _sfxVolumeSlider.RegisterValueChangedCallback(HandleSfxVolumeChanged);
        }

        _displayDropdown = Root.Q<DropdownField>("Dropdown_Display");
        if (_displayDropdown != null)
        {
            _displayDropdown.RegisterValueChangedCallback(HandleDisplayChanged);
        }

        _resolutionDropdown = Root.Q<DropdownField>("Dropdown_Resolution");
        if (_resolutionDropdown != null)
        {
            _resolutionDropdown.RegisterValueChangedCallback(HandleResolutionChanged);
        }

        _windowModeDropdown = Root.Q<DropdownField>("Dropdown_WindowMode");
        if (_windowModeDropdown != null)
        {
            _windowModeDropdown.RegisterValueChangedCallback(HandleWindowModeChanged);
        }

        _applyDisplayButton = Root.Q<Button>("Btn_ApplyDisplay");
        if (_applyDisplayButton != null)
        {
            _applyDisplayButton.clicked += HandleApplyDisplayClicked;
        }

        _displayConfirmationOverlay = Root.Q<VisualElement>("DisplayConfirmationOverlay");
        _displayConfirmationMessage = Root.Q<Label>("Label_DisplayConfirmationMessage");
        _keepDisplayButton = Root.Q<Button>("Btn_KeepDisplay");
        if (_keepDisplayButton != null)
        {
            _keepDisplayButton.clicked += HandleKeepDisplayClicked;
        }

        _revertDisplayButton = Root.Q<Button>("Btn_RevertDisplay");
        if (_revertDisplayButton != null)
        {
            _revertDisplayButton.clicked += HandleRevertDisplayClicked;
        }

        HideDisplayConfirmation();

        _lootMagnetToggle = Root.Q<Toggle>("Toggle_LootMagnet");
        if (_lootMagnetToggle != null)
        {
            _lootMagnetToggle.RegisterValueChangedCallback(HandleLootMagnetToggled);
        }

        _damageNumbersToggle = Root.Q<Toggle>("Toggle_DamageNumbers");
        if (_damageNumbersToggle != null)
        {
            _damageNumbersToggle.RegisterValueChangedCallback(HandleDamageNumbersToggled);
        }

        _showFpsToggle = Root.Q<Toggle>("Toggle_ShowFps");
        if (_showFpsToggle != null)
        {
            _showFpsToggle.RegisterValueChangedCallback(HandleShowFpsToggled);
        }

        ShowMainTab();
    }

    public void SetVolumeValues(float masterVolume, float musicVolume, float sfxVolume)
    {
        _masterVolumeSlider?.SetValueWithoutNotify(masterVolume);
        _musicVolumeSlider?.SetValueWithoutNotify(musicVolume);
        _sfxVolumeSlider?.SetValueWithoutNotify(sfxVolume);
    }

    public void SetDisplayOptions(IList<string> options, int selectedIndex)
    {
        SetDropdownOptions(_displayDropdown, options, selectedIndex);
    }

    public void SetResolutionOptions(IList<string> options, int selectedIndex)
    {
        SetDropdownOptions(_resolutionDropdown, options, selectedIndex);
    }

    public void SetWindowModeOptions(IList<string> options, int selectedIndex)
    {
        SetDropdownOptions(_windowModeDropdown, options, selectedIndex);
    }

    public void SetDisplayApplyEnabled(bool enabled)
    {
        _applyDisplayButton?.SetEnabled(enabled);
    }

    public void SetDisplayControlsEnabled(bool enabled)
    {
        _displayDropdown?.SetEnabled(enabled);
        _resolutionDropdown?.SetEnabled(enabled);
        _windowModeDropdown?.SetEnabled(enabled);
    }

    public void SetDisplayControlEnabled(bool enabled)
    {
        _displayDropdown?.SetEnabled(enabled);
    }

    public void SetResolutionControlEnabled(bool enabled)
    {
        _resolutionDropdown?.SetEnabled(enabled);
    }

    public void SetWindowModeControlEnabled(bool enabled)
    {
        _windowModeDropdown?.SetEnabled(enabled);
    }

    public void ShowDisplayConfirmation(string message)
    {
        SetDisplayConfirmationMessage(message);

        if (_displayConfirmationOverlay != null)
        {
            _displayConfirmationOverlay.style.display = DisplayStyle.Flex;
        }

        FocusWhenReady(_revertDisplayButton);
    }

    public void SetDisplayConfirmationMessage(string message)
    {
        if (_displayConfirmationMessage != null)
        {
            _displayConfirmationMessage.text = message ?? string.Empty;
        }
    }

    public void HideDisplayConfirmation()
    {
        if (_displayConfirmationOverlay != null)
        {
            _displayConfirmationOverlay.style.display = DisplayStyle.None;
        }

        FocusWhenReady(_applyDisplayButton);
    }

    public void SetLootMagnetValue(bool enabled)
    {
        _lootMagnetToggle?.SetValueWithoutNotify(enabled);
    }

    public void SetDamageNumbersValue(bool enabled)
    {
        _damageNumbersToggle?.SetValueWithoutNotify(enabled);
    }

    public void SetShowFpsValue(bool enabled)
    {
        _showFpsToggle?.SetValueWithoutNotify(enabled);
    }

    public void SetControlBindings(
        IList<InputBindingDisplayEntry> keyboardEntries,
        IList<InputBindingDisplayEntry> gamepadEntries,
        string activeRebindId)
    {
        if (_keyboardControlBindingsList == null && _gamepadControlBindingsList == null)
        {
            return;
        }

        _keyboardControlBindingsList?.Clear();
        _gamepadControlBindingsList?.Clear();
        _controlRebindButtons.Clear();
        _controlResetButtons.Clear();

        bool hasActiveRebind = !string.IsNullOrEmpty(activeRebindId);
        AddControlBindingRows(_keyboardControlBindingsList, keyboardEntries, activeRebindId, hasActiveRebind);

        bool hasGamepadEntries = gamepadEntries != null && gamepadEntries.Count > 0;
        if (_gamepadControlSection != null)
        {
            _gamepadControlSection.style.display = hasGamepadEntries ? DisplayStyle.Flex : DisplayStyle.None;
        }

        if (hasGamepadEntries)
        {
            AddControlBindingRows(_gamepadControlBindingsList, gamepadEntries, activeRebindId, hasActiveRebind);
        }

        _resetAllControlsButton?.SetEnabled(!hasActiveRebind);
        _mainTab?.SetEnabled(!hasActiveRebind);
        _controlsTab?.SetEnabled(!hasActiveRebind);
    }

    public void SetControlRebindStatus(string statusText)
    {
        if (_controlRebindStatus == null)
        {
            return;
        }

        _controlRebindStatus.text = statusText ?? string.Empty;
        _controlRebindStatus.style.display = string.IsNullOrWhiteSpace(statusText)
            ? DisplayStyle.None
            : DisplayStyle.Flex;
    }

    public override void Show()
    {
        ShowMainTab();
        base.Show();
    }

    public void ShowMainTab()
    {
        ShowTab(_mainTab, _mainContent);
    }

    public void ShowControlsTab()
    {
        ShowTab(_controlsTab, _controlsContent);
    }

    private void HandleCloseClicked() => OnCloseClicked?.Invoke();
    private void HandleMainTabClicked(ClickEvent evt) => ShowMainTab();
    private void HandleControlsTabClicked(ClickEvent evt) => ShowControlsTab();
    private void HandleMasterVolumeChanged(ChangeEvent<float> evt) => OnMasterVolumeChanged?.Invoke(evt.newValue);
    private void HandleMusicVolumeChanged(ChangeEvent<float> evt) => OnMusicVolumeChanged?.Invoke(evt.newValue);
    private void HandleSfxVolumeChanged(ChangeEvent<float> evt) => OnSFXVolumeChanged?.Invoke(evt.newValue);
    private void HandleDisplayChanged(ChangeEvent<string> evt) => OnDisplaySelected?.Invoke(_displayDropdown.index);
    private void HandleResolutionChanged(ChangeEvent<string> evt) => OnResolutionSelected?.Invoke(_resolutionDropdown.index);
    private void HandleWindowModeChanged(ChangeEvent<string> evt) => OnWindowModeSelected?.Invoke(_windowModeDropdown.index);
    private void HandleApplyDisplayClicked() => OnApplyDisplayClicked?.Invoke();
    private void HandleKeepDisplayClicked() => OnKeepDisplayClicked?.Invoke();
    private void HandleRevertDisplayClicked() => OnRevertDisplayClicked?.Invoke();
    private void HandleLootMagnetToggled(ChangeEvent<bool> evt) => OnLootMagnetToggled?.Invoke(evt.newValue);
    private void HandleDamageNumbersToggled(ChangeEvent<bool> evt) => OnDamageNumbersToggled?.Invoke(evt.newValue);
    private void HandleShowFpsToggled(ChangeEvent<bool> evt) => OnShowFpsToggled?.Invoke(evt.newValue);
    private void HandleResetAllControlsClicked() => OnResetAllControlsRequested?.Invoke();

    private static void FocusWhenReady(VisualElement element)
    {
        if (ControllerFeatureGate.IsEnabled)
        {
            element?.schedule.Execute(() => element.Focus()).ExecuteLater(0);
        }
    }

    public override void Dispose()
    {
        if (_closeButton != null) _closeButton.clicked -= HandleCloseClicked;
        if (_mainTab != null) _mainTab.UnregisterCallback<ClickEvent>(HandleMainTabClicked);
        if (_controlsTab != null) _controlsTab.UnregisterCallback<ClickEvent>(HandleControlsTabClicked);
        if (_masterVolumeSlider != null) _masterVolumeSlider.UnregisterValueChangedCallback(HandleMasterVolumeChanged);
        if (_musicVolumeSlider != null) _musicVolumeSlider.UnregisterValueChangedCallback(HandleMusicVolumeChanged);
        if (_sfxVolumeSlider != null) _sfxVolumeSlider.UnregisterValueChangedCallback(HandleSfxVolumeChanged);
        if (_displayDropdown != null) _displayDropdown.UnregisterValueChangedCallback(HandleDisplayChanged);
        if (_resolutionDropdown != null) _resolutionDropdown.UnregisterValueChangedCallback(HandleResolutionChanged);
        if (_windowModeDropdown != null) _windowModeDropdown.UnregisterValueChangedCallback(HandleWindowModeChanged);
        if (_applyDisplayButton != null) _applyDisplayButton.clicked -= HandleApplyDisplayClicked;
        if (_keepDisplayButton != null) _keepDisplayButton.clicked -= HandleKeepDisplayClicked;
        if (_revertDisplayButton != null) _revertDisplayButton.clicked -= HandleRevertDisplayClicked;
        if (_lootMagnetToggle != null) _lootMagnetToggle.UnregisterValueChangedCallback(HandleLootMagnetToggled);
        if (_damageNumbersToggle != null) _damageNumbersToggle.UnregisterValueChangedCallback(HandleDamageNumbersToggled);
        if (_showFpsToggle != null) _showFpsToggle.UnregisterValueChangedCallback(HandleShowFpsToggled);
        if (_resetAllControlsButton != null) _resetAllControlsButton.clicked -= HandleResetAllControlsClicked;
        base.Dispose();
    }

    private void AddControlBindingRows(
        VisualElement bindingsList,
        IList<InputBindingDisplayEntry> entries,
        string activeRebindId,
        bool hasActiveRebind)
    {
        if (bindingsList == null || entries == null)
        {
            return;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            AddControlBindingRow(bindingsList, entries[i], activeRebindId, hasActiveRebind);
        }
    }

    private void AddControlBindingRow(
        VisualElement bindingsList,
        InputBindingDisplayEntry entry,
        string activeRebindId,
        bool hasActiveRebind)
    {
        string entryId = entry.Id;
        bool isActiveRebind = string.Equals(entryId, activeRebindId, StringComparison.Ordinal);

        VisualElement row = new VisualElement();
        row.AddToClassList("settings-control-row");

        Label actionLabel = new Label(entry.DisplayName);
        actionLabel.AddToClassList("settings-control-action");
        row.Add(actionLabel);

        Label bindingLabel = new Label(isActiveRebind ? "Listening..." : entry.BindingLabel);
        bindingLabel.AddToClassList("settings-control-binding");
        row.Add(bindingLabel);

        Button rebindButton = new Button(() => OnRebindControlRequested?.Invoke(entryId))
        {
            text = isActiveRebind ? "Listening" : "Rebind"
        };
        rebindButton.AddToClassList("standard-button");
        rebindButton.AddToClassList("settings-control-button");
        rebindButton.SetEnabled(!hasActiveRebind);
        row.Add(rebindButton);
        _controlRebindButtons[entryId] = rebindButton;

        Button resetButton = new Button(() => OnResetControlRequested?.Invoke(entryId))
        {
            text = "Reset"
        };
        resetButton.AddToClassList("standard-button");
        resetButton.AddToClassList("settings-control-button");
        resetButton.SetEnabled(!hasActiveRebind && entry.HasOverride);
        row.Add(resetButton);
        _controlResetButtons[entryId] = resetButton;

        bindingsList.Add(row);
    }

    private void ShowTab(VisualElement activeTab, VisualElement activeContent)
    {
        _mainTab?.RemoveFromClassList(ActiveTabClass);
        _controlsTab?.RemoveFromClassList(ActiveTabClass);
        activeTab?.AddToClassList(ActiveTabClass);

        if (_mainContent != null)
        {
            _mainContent.style.display = activeContent == _mainContent ? DisplayStyle.Flex : DisplayStyle.None;
        }

        if (_controlsContent != null)
        {
            _controlsContent.style.display = activeContent == _controlsContent ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    private static void SetDropdownOptions(DropdownField dropdown, IList<string> options, int selectedIndex)
    {
        if (dropdown == null)
        {
            return;
        }

        List<string> choices = options != null
            ? new List<string>(options)
            : new List<string>();

        dropdown.choices = choices;

        if (choices.Count == 0)
        {
            dropdown.SetValueWithoutNotify(string.Empty);
            return;
        }

        int safeIndex = selectedIndex;
        if (safeIndex < 0 || safeIndex >= choices.Count)
        {
            safeIndex = 0;
        }

        dropdown.SetValueWithoutNotify(choices[safeIndex]);
    }
}
