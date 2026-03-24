using UnityEngine;
using UnityEngine.UIElements;
using OutlandHaven.UIToolkit;

namespace OutlandHaven.Inventory
{
    public class InventorySlotView
    {
        private VisualElement _root;
        private Image _icon;
        private Label _qtyLabel;

        private InventorySlot _slotData;
        private InventoryManager _owningContainer;
        private UIInventoryEventsSO _uiInventoryEvents;

        // Drag and Drop State
        private bool _isDragging = false;
        private Vector2 _dragStartPosition;
        private const float DragThreshold = 10f; // Pixels to move before initiating drag

        public InventorySlotView(VisualElement root, InventoryManager owningContainer, UIInventoryEventsSO uiInventoryEvents)
        {
            _root = root;
            _owningContainer = owningContainer;
            _uiInventoryEvents = uiInventoryEvents;

            _icon = root.Q<Image>("slot-icon");
            _qtyLabel = root.Q<Label>("slot-qty");

            // Register pointer callbacks
            _root.RegisterCallback<PointerDownEvent>(OnPointerDown);
            _root.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            _root.RegisterCallback<PointerUpEvent>(OnPointerUp);
        }

        public void Update(InventorySlot slotData)
        {
            _slotData = slotData;

            // Store the target slot and container in the VisualElement's userData
            // so we can resolve the drop target in OnPointerUp.
            _root.userData = new SlotDropData { Slot = _slotData, Container = _owningContainer };

            if (slotData == null || slotData.IsEmpty)
            {
                _icon.sprite = null;
                _icon.style.display = DisplayStyle.None;
                _qtyLabel.text = "";
            }
            else
            {
                _icon.sprite = slotData.HeldItem.BaseItem.Icon;
                _icon.style.display = DisplayStyle.Flex;
                _qtyLabel.text = slotData.Count > 1 ? slotData.Count.ToString() : "";
            }
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (_slotData == null || _slotData.IsEmpty || evt.button != 0) return;

            // Do not initiate visual drag right away (wait for threshold)
            _isDragging = false;
            _dragStartPosition = evt.position;

            _root.CapturePointer(evt.pointerId);
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!_root.HasPointerCapture(evt.pointerId)) return;

            // If we are not currently dragging, check distance against threshold
            if (!_isDragging)
            {
                float distance = Vector2.Distance(_dragStartPosition, evt.position);
                if (distance >= DragThreshold)
                {
                    _isDragging = true;
                    // Provide the icon dimensions as the payload for sizing the ghost layer
                    Vector2 iconSize = new Vector2(_icon.layout.width, _icon.layout.height);
                    UIDragManager.Instance?.StartDrag(_slotData.HeldItem.BaseItem.Icon, evt.position, iconSize);
                }
            }
            else
            {
                // We are already dragging, so keep updating the absolute position
                UIDragManager.Instance?.UpdateDrag(evt.position);
            }
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (!_root.HasPointerCapture(evt.pointerId)) return;

            _root.ReleasePointer(evt.pointerId);

            if (_isDragging)
            {
                // Stop dragging state and clear visual ghost
                _isDragging = false;
                UIDragManager.Instance?.StopDrag();

                // Pick the element underneath the pointer to find the drop target
                VisualElement targetElement = _root.panel.Pick(evt.position);

                // Assuming the drop target might be a nested element (like an icon or label) inside the root
                // Traverse up to find the element containing our SlotDropData.
                SlotDropData targetData = FindTargetDropData(targetElement);

                if (targetData != null && targetData.Container != null && targetData.Slot != null)
                {
                    if (targetData.Slot != _slotData || targetData.Container != _owningContainer)
                    {
                        // Invoke the cross-container swap logic
                        _uiInventoryEvents?.OnRequestMoveItem?.Invoke(_owningContainer, _slotData, targetData.Container, targetData.Slot);
                    }
                }
            }
            else
            {
                // Pointer did not move past the threshold, treat as a normal click
                if (evt.button == 0 && _slotData != null && !_slotData.IsEmpty)
                {
                    _uiInventoryEvents?.OnItemClicked?.Invoke(_slotData);
                }
            }
        }

        private SlotDropData FindTargetDropData(VisualElement element)
        {
            while (element != null)
            {
                if (element.userData is SlotDropData data)
                {
                    return data;
                }
                element = element.parent;
            }
            return null;
        }
    }

    public class SlotDropData
    {
        public InventorySlot Slot { get; set; }
        public InventoryManager Container { get; set; }
    }
}