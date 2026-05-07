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
    private Button _deleteButton;

    public event Action<int> OnSlotSelected;
    public event Action<int> OnDeleteRequested;
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
        _deleteButton = m_TopElement.Q<Button>("Btn_Delete");
    }

    private void HandleSlotClicked()
    {
        OnSlotSelected?.Invoke(_slotIndex);
    }

    private void HandleDeleteClicked(ClickEvent evt)
    {
        // Prevent the parent Btn_SlotFrame from receiving this click
        evt.StopImmediatePropagation();
        
        OnDeleteRequested?.Invoke(_slotIndex);
    }

    protected override void RegisterButtonCallbacks()
    {
        if (_slotButton != null)
            _slotButton.clicked += HandleSlotClicked;

        if (_deleteButton != null)
            _deleteButton.RegisterCallback<ClickEvent>(HandleDeleteClicked);
    }

    // Injects data from the Save System DTO
    public void SetData(SaveSlotData data)
    {
        if (_indexLabel != null) _indexLabel.text = $"Slot {data.SlotIndex}";
        if (_levelLabel != null) _levelLabel.text = data.Level.ToString();
        if (_goldLabel != null) _goldLabel.text = data.Gold.ToString();
        if (_timestampLabel != null) _timestampLabel.text = data.Timestamp;

        // Handle delete button state
        if (_deleteButton != null)
        {
            bool isEmpty = data.Timestamp == "Empty Slot" || data.Timestamp == "Unknown";
            
            // Keep it visible but disable interaction
            _deleteButton.SetEnabled(!isEmpty);
            _deleteButton.pickingMode = isEmpty ? PickingMode.Ignore : PickingMode.Position;
            
            if (isEmpty)
            {
                _deleteButton.AddToClassList("save-slot__delete-btn--disabled");
            }
            else
            {
                _deleteButton.RemoveFromClassList("save-slot__delete-btn--disabled");
            }
        }
    }

    public override void Dispose()
    {
        if (_slotButton != null)
            _slotButton.clicked -= HandleSlotClicked;

        if (_deleteButton != null)
            _deleteButton.UnregisterCallback<ClickEvent>(HandleDeleteClicked);

        base.Dispose();
    }
}