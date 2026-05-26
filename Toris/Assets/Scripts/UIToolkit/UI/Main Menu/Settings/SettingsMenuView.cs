using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using OutlandHaven.UIToolkit;

public class SettingsMenuView : GameView
{
    public override ScreenType ID => ScreenType.SettingsModal;

    private Button _closeButton;
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

    public SettingsMenuView(VisualElement topElement, UIEventsSO uiEvents) : base(topElement, uiEvents) { }

    public override void Initialize()
    {
        base.Initialize();
        _closeButton = Root.Q<Button>("Btn_Close");
        if (_closeButton != null) _closeButton.clicked += HandleCloseClicked;

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
    }

    public void SetLootMagnetValue(bool enabled)
    {
        _lootMagnetToggle?.SetValueWithoutNotify(enabled);
    }

    private void HandleCloseClicked() => OnCloseClicked?.Invoke();
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

    public override void Dispose()
    {
        if (_closeButton != null) _closeButton.clicked -= HandleCloseClicked;
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
        base.Dispose();
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
