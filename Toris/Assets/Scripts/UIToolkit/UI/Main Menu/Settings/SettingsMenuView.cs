using System;
using UnityEngine.UIElements;
using OutlandHaven.UIToolkit;

public class SettingsMenuView : GameView
{
    public override ScreenType ID => ScreenType.SettingsModal;

    private Button _closeButton;
    public event Action OnCloseClicked;

    public SettingsMenuView(VisualElement topElement, UIEventsSO uiEvents) : base(topElement, uiEvents) { }

    public override void Initialize()
    {
        base.Initialize();
        _closeButton = Root.Q<Button>("Btn_Close");
        if (_closeButton != null) _closeButton.clicked += HandleCloseClicked;
    }

    private void HandleCloseClicked() => OnCloseClicked?.Invoke();

    public override void Dispose()
    {
        if (_closeButton != null) _closeButton.clicked -= HandleCloseClicked;
        base.Dispose();
    }
}