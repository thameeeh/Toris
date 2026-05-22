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
    }

    public void SetVolumeValues(float masterVolume, float musicVolume, float sfxVolume)
    {
        _masterVolumeSlider?.SetValueWithoutNotify(masterVolume);
        _musicVolumeSlider?.SetValueWithoutNotify(musicVolume);
        _sfxVolumeSlider?.SetValueWithoutNotify(sfxVolume);
    }

    private void HandleCloseClicked() => OnCloseClicked?.Invoke();
    private void HandleMasterVolumeChanged(ChangeEvent<float> evt) => OnMasterVolumeChanged?.Invoke(evt.newValue);
    private void HandleMusicVolumeChanged(ChangeEvent<float> evt) => OnMusicVolumeChanged?.Invoke(evt.newValue);
    private void HandleSfxVolumeChanged(ChangeEvent<float> evt) => OnSFXVolumeChanged?.Invoke(evt.newValue);

    public override void Dispose()
    {
        if (_closeButton != null) _closeButton.clicked -= HandleCloseClicked;
        if (_masterVolumeSlider != null) _masterVolumeSlider.UnregisterValueChangedCallback(HandleMasterVolumeChanged);
        if (_musicVolumeSlider != null) _musicVolumeSlider.UnregisterValueChangedCallback(HandleMusicVolumeChanged);
        if (_sfxVolumeSlider != null) _sfxVolumeSlider.UnregisterValueChangedCallback(HandleSfxVolumeChanged);
        base.Dispose();
    }
}
