using System;
using UnityEngine;
using UnityEngine.UIElements;
using OutlandHaven.UIToolkit;

public class MainMenuView : GameView
{
    public override ScreenType ID => ScreenType.MainMenu;

    // Visual Elements
    private Button _playButton;
    private Button _settingsButton;
    private Button _exitButton;
    private ScrollView _saveSlotsContainer;

    public event Action OnPlayClicked;
    public event Action OnSettingsClicked;
    public event Action OnExitClicked;

    public MainMenuView(VisualElement topElement, UIEventsSO uiEvents) : base(topElement, uiEvents) { }

    public override void Initialize()
    {
        base.Initialize();

        _playButton = Root.Q<Button>("Btn_Play");
        _settingsButton = Root.Q<Button>("Btn_Settings");
        _exitButton = Root.Q<Button>("Btn_Exit");

        // --- ADDED VALIDATION ---
        _saveSlotsContainer = Root.Q<ScrollView>("Container_SaveSlots");

        if (_saveSlotsContainer == null)
        {
            Debug.LogError("CRITICAL UI ERROR: MainMenuView could not find 'Container_SaveSlots'.");
        }
        else
        {
            ToggleSaveSlots(false);
        }
        // ------------------------

        if (_playButton != null) _playButton.clicked += HandlePlayClicked;
        if (_settingsButton != null) _settingsButton.clicked += HandleSettingsClicked;
        if (_exitButton != null) _exitButton.clicked += HandleExitClicked;
    }

    // ADDED: Let the View handle adding elements to its own DOM
    public void AddSaveSlot(VisualElement slotElement)
    {
        if (_saveSlotsContainer != null) _saveSlotsContainer.Add(slotElement);
    }

    // ADDED: Let the View handle clearing its own DOM
    public void ClearSaveSlots()
    {
        if (_saveSlotsContainer != null) _saveSlotsContainer.Clear();
    }

    // ADDED: MVP compliant method to change UI state[cite: 7]
    public void ToggleSaveSlots(bool isVisible)
    {
        if (_saveSlotsContainer != null)
        {
            _saveSlotsContainer.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    private void HandlePlayClicked() => OnPlayClicked?.Invoke();
    private void HandleSettingsClicked() => OnSettingsClicked?.Invoke();
    private void HandleExitClicked() => OnExitClicked?.Invoke();

    public override void Dispose()
    {
        if (_playButton != null) _playButton.clicked -= HandlePlayClicked;
        if (_settingsButton != null) _settingsButton.clicked -= HandleSettingsClicked;
        if (_exitButton != null) _exitButton.clicked -= HandleExitClicked;
        base.Dispose();
    }
}