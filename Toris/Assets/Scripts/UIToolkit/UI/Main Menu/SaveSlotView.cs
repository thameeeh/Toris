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
    public void SetData(int level, int gold, string timestamp)
    {
        if (_indexLabel != null) _indexLabel.text = $"Slot {_slotIndex}";
        if (_levelLabel != null) _levelLabel.text = level.ToString();
        if (_goldLabel != null) _goldLabel.text = gold.ToString();
        if (_timestampLabel != null) _timestampLabel.text = timestamp;
    }

    public override void Dispose()
    {
        if (_slotButton != null)
            _slotButton.clicked -= HandleSlotClicked;

        base.Dispose();
    }
}