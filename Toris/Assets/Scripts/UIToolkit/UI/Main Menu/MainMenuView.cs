using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UIElements;
using OutlandHaven.UIToolkit;

public class MainMenuView : GameView
{
    public override ScreenType ID => ScreenType.MainMenu;

    // Visual Elements
    private Button _playButton;
    private Button _settingsButton;
    private Button _exitButton;
    private Button _closeSlotsButton;
    private VisualElement _saveSlotsPanel;
    private ScrollView _saveSlotsContainer;

    // Sub-View Management
    private VisualTreeAsset _saveSlotTemplate;
    private List<SaveSlotView> _slotViews = new List<SaveSlotView>();

    public event Action OnPlayClicked;
    public event Action OnSettingsClicked;
    public event Action OnExitClicked;
    public event Action OnCloseSlotsClicked;
    public event Action<int> OnSaveSlotSelected;
    public event Action<int> OnSaveSlotDeleteRequested;

    public MainMenuView(VisualElement topElement, UIEventsSO uiEvents) : base(topElement, uiEvents) { }

    public override void Initialize()
    {
        base.Initialize();

        _playButton = Root.Q<Button>("Btn_Play");
        _settingsButton = Root.Q<Button>("Btn_Settings");
        _exitButton = Root.Q<Button>("Btn_Exit");
        _closeSlotsButton = Root.Q<Button>("Btn_CloseSlots");

        // --- ADDED VALIDATION ---
        _saveSlotsPanel = Root.Q<VisualElement>("Panel_SaveSlots");
        _saveSlotsContainer = Root.Q<ScrollView>("Container_SaveSlots");

        if (_saveSlotsPanel == null)
        {
            Debug.LogError("CRITICAL UI ERROR: MainMenuView could not find 'Panel_SaveSlots'.");
        }
        else
        {
            ToggleSaveSlots(false);
        }
        // ------------------------

        if (_playButton != null) _playButton.clicked += HandlePlayClicked;
        if (_settingsButton != null) _settingsButton.clicked += HandleSettingsClicked;
        if (_exitButton != null) _exitButton.clicked += HandleExitClicked;
        if (_closeSlotsButton != null) _closeSlotsButton.clicked += HandleCloseSlotsClicked;
    }

    public void SetSaveSlotTemplate(VisualTreeAsset template) => _saveSlotTemplate = template;

    public void PopulateSaveSlots(System.Collections.Generic.IEnumerable<SaveSlotData> slots)
    {
        ClearSaveSlots();

        if (_saveSlotTemplate == null) return;

        foreach (var data in slots)
        {
            TemplateContainer slotInstance = _saveSlotTemplate.Instantiate();
            slotInstance.AddToClassList("save-slot-wrapper");

            SaveSlotView slotView = new SaveSlotView(slotInstance, data.SlotIndex);
            slotView.Initialize();
            slotView.Show();
            slotView.SetData(data);

            slotView.OnSlotSelected += HandleSlotSelected;
            slotView.OnDeleteRequested += HandleDeleteRequested;
            _slotViews.Add(slotView);
            _saveSlotsContainer.contentContainer.Add(slotInstance);
        }
    }

    private void HandleSlotSelected(int index) => OnSaveSlotSelected?.Invoke(index);
    private void HandleDeleteRequested(int index) => OnSaveSlotDeleteRequested?.Invoke(index);

    // ADDED: Let the View handle clearing its own DOM and disposing sub-views
    public void ClearSaveSlots()
    {
        foreach (var view in _slotViews)
        {
            view.OnSlotSelected -= HandleSlotSelected;
            view.OnDeleteRequested -= HandleDeleteRequested;
            view.Dispose();
        }
        _slotViews.Clear();

        if (_saveSlotsContainer != null) _saveSlotsContainer.contentContainer.Clear();
    }

    // ADDED: MVP compliant method to change UI state[cite: 7]
    public void ToggleSaveSlots(bool isVisible)
    {
        if (_saveSlotsPanel != null)
        {
            _saveSlotsPanel.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        VisualElement focusTarget = _playButton;
        if (isVisible)
        {
            focusTarget = _saveSlotsContainer?.Q<Button>("Btn_SlotFrame") ?? _closeSlotsButton;
        }

        FocusWhenReady(focusTarget);
    }

    private void HandlePlayClicked() => OnPlayClicked?.Invoke();
    private void HandleSettingsClicked() => OnSettingsClicked?.Invoke();
    private void HandleExitClicked() => OnExitClicked?.Invoke();
    private void HandleCloseSlotsClicked() => OnCloseSlotsClicked?.Invoke();

    private static void FocusWhenReady(VisualElement element)
    {
        if (ControllerFeatureGate.IsEnabled)
        {
            element?.schedule.Execute(() => element.Focus()).ExecuteLater(0);
        }
    }

    public override void Dispose()
    {
        ClearSaveSlots();
        if (_playButton != null) _playButton.clicked -= HandlePlayClicked;
        if (_settingsButton != null) _settingsButton.clicked -= HandleSettingsClicked;
        if (_exitButton != null) _exitButton.clicked -= HandleExitClicked;
        if (_closeSlotsButton != null) _closeSlotsButton.clicked -= HandleCloseSlotsClicked;
        base.Dispose();
    }
}
