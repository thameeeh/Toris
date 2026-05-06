using System;
using UnityEngine.UIElements;
using OutlandHaven.UIToolkit; // Assuming your namespace

public class MainMenuView : GameView
{
    public override ScreenType ID => ScreenType.MainMenu;

    // Visual Elements
    private Button _playButton;
    private Button _settingsButton;
    private Button _exitButton;

    // Semantic Intents (Actions for the Controller to listen to)
    public event Action OnPlayClicked;
    public event Action OnSettingsClicked;
    public event Action OnExitClicked;

    // CORRECTED: Constructor now matches GameView.cs requirements[cite: 10]
    public MainMenuView(VisualElement topElement, UIEventsSO uiEvents) : base(topElement, uiEvents)
    {
    }

    public override void Initialize()
    {
        base.Initialize(); // Let UIView handle m_HideOnAwake logic[cite: 11]

        // Query elements using the name attribute as a strict lookup key[cite: 6]
        _playButton = Root.Q<Button>("Btn_Play");
        _settingsButton = Root.Q<Button>("Btn_Settings");
        _exitButton = Root.Q<Button>("Btn_Exit");

        // Bind UI Toolkit hardware events
        if (_playButton != null) _playButton.clicked += HandlePlayClicked;
        if (_settingsButton != null) _settingsButton.clicked += HandleSettingsClicked;
        if (_exitButton != null) _exitButton.clicked += HandleExitClicked;
    }

    // Translating hardware clicks into pure C# intents[cite: 7]
    private void HandlePlayClicked() => OnPlayClicked?.Invoke();
    private void HandleSettingsClicked() => OnSettingsClicked?.Invoke();
    private void HandleExitClicked() => OnExitClicked?.Invoke();

    // IDisposable cleanup[cite: 7, 11]
    public override void Dispose()
    {
        if (_playButton != null) _playButton.clicked -= HandlePlayClicked;
        if (_settingsButton != null) _settingsButton.clicked -= HandleSettingsClicked;
        if (_exitButton != null) _exitButton.clicked -= HandleExitClicked;

        base.Dispose();
    }
}