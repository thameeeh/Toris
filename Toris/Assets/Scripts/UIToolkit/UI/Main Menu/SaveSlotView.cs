using System;
using UnityEngine.UIElements;
using OutlandHaven.UIToolkit;

public class SaveSlotView : UIView
{
    private Label _levelLabel;
    private Label _goldLabel;
    private Label _timestampLabel;
    private Label _indexLabel;
    private Button _slotButton;

    public event Action<int> OnSlotSelected;
    private int _slotIndex;

    public SaveSlotView(VisualElement root, int index) : base(root)
    {
        _slotIndex = index;
        m_HideOnAwake = false;
    }

    protected override void SetVisualElements()
    {
        _levelLabel = m_TopElement.Q<Label>("Label_Level");
        _goldLabel = m_TopElement.Q<Label>("Label_Gold");
        _timestampLabel = m_TopElement.Q<Label>("Label_Timestamp");
        _indexLabel = m_TopElement.Q<Label>("Label_SlotIndex");
        _slotButton = m_TopElement.Q<Button>("Btn_SlotFrame");
    }

    private void HandleSlotClicked()
    {
        OnSlotSelected?.Invoke(_slotIndex);
    }

    protected override void RegisterButtonCallbacks()
    {
        if (_slotButton != null)
            _slotButton.clicked += HandleSlotClicked; // Subscribe
    }

    // Injects data from the Save System DTO[cite: 1]
    public void SetData(SaveSlotData data)
    {
        if (_indexLabel != null) _indexLabel.text = $"Slot {data.SlotIndex}";
        if (_levelLabel != null) _levelLabel.text = data.Level.ToString();
        if (_goldLabel != null) _goldLabel.text = data.Gold.ToString();
        if (_timestampLabel != null) _timestampLabel.text = data.Timestamp;
    }

    public override void Dispose()
    {
        if (_slotButton != null)
            _slotButton.clicked -= HandleSlotClicked;

        base.Dispose();
    }
}