using System;
using UnityEngine.UIElements;
using OutlandHaven.UIToolkit;

public class SettingsMenuView : GameView
{
    public override ScreenType ID => ScreenType.SettingsModal;

    private Button _closeButton;
    private Slider _masterVolumeSlider;
    private Slider _musicVolumeSlider;
    private Slider _sfxVolumeSlider;

    public event Action OnCloseClicked;
    public event Action<float> OnMasterVolumeChanged;
    public event Action<float> OnMusicVolumeChanged;
    public event Action<float> OnSFXVolumeChanged;

    public SettingsMenuView(VisualElement topElement, UIEventsSO uiEvents) : base(topElement, uiEvents) { }

    public override void Initialize()
    {
        base.Initialize();
        _closeButton = Root.Q<Button>("Btn_Close");
        if (_closeButton != null) _closeButton.clicked += HandleCloseClicked;

        _masterVolumeSlider = Root.Q<Slider>("Slider_MasterVolume");
        if (_masterVolumeSlider != null)
        {
            _masterVolumeSlider.RegisterValueChangedCallback(evt => OnMasterVolumeChanged?.Invoke(evt.newValue));
        }

        _musicVolumeSlider = Root.Q<Slider>("Slider_MusicVolume");
        if (_musicVolumeSlider != null)
        {
            _musicVolumeSlider.RegisterValueChangedCallback(evt => OnMusicVolumeChanged?.Invoke(evt.newValue));
        }

        _sfxVolumeSlider = Root.Q<Slider>("Slider_SFXVolume");
        if (_sfxVolumeSlider != null)
        {
            _sfxVolumeSlider.RegisterValueChangedCallback(evt => OnSFXVolumeChanged?.Invoke(evt.newValue));
        }
    }

    private void HandleCloseClicked() => OnCloseClicked?.Invoke();

    public override void Dispose()
    {
        if (_closeButton != null) _closeButton.clicked -= HandleCloseClicked;
        // Note: For sliders, unregistering value changed callbacks is complex without keeping the exact delegate reference.
        // Given GameView lifecycle, the UI document itself gets destroyed, so this isn't strictly necessary for sliders here unless they're reused.
        base.Dispose();
    }
}