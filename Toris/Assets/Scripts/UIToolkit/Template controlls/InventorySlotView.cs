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
            if (_root.userData is string proxySlotID)
            {
                // Preserve the proxySlotID if it exists on the root
                _root.userData = proxySlotID;
            }
            else
            {
                _root.userData = new SlotDropData { Slot = _slotData, Container = _owningContainer };
            }

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
                _icon.scaleMode = ScaleMode.ScaleToFit;
                _qtyLabel.text = slotData.Count > 1 ? slotData.Count.ToString() : "";
            }
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (_slotData == null || _slotData.IsEmpty) return;
            // Only allow left click (0) and right click (1) to capture pointer
            if (evt.button != 0 && evt.button != 1) return;

            // Do not initiate visual drag right away (wait for threshold)
            _isDragging = false;
            _dragStartPosition = evt.position;

            _root.CapturePointer(evt.pointerId);
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!_root.HasPointerCapture(evt.pointerId)) return;

            // Only left clicks can initiate dragging
            if (evt.button != 0) return;

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

            if (evt.button == 1)
            {
                // Stop dragging state and clear visual ghost if any
                _isDragging = false;
                UIDragManager.Instance?.StopDrag();

                // Fire right click event
                if (_slotData != null && !_slotData.IsEmpty)
                {
                    _uiInventoryEvents?.OnItemRightClicked?.Invoke(_slotData);
                }
                return;
            }

            if (_isDragging)
            {
                // Stop dragging state and clear visual ghost
                _isDragging = false;
                UIDragManager.Instance?.StopDrag();

                // 1. Perform the raycast to pick the element underneath the pointer
                VisualElement targetElement = _root.panel.Pick(evt.position);

                // 2. Add this Debug.Log to see exactly what UI Toolkit hit
                if (targetElement != null)
                {
                    Debug.Log($"[Drag Test] Picked Element Name: '{targetElement.name}', Type: '{targetElement.GetType().Name}'");

                    // Optional: If you want to see the hierarchy of what was hit to make sure you 
                    // aren't hitting a child element (like an icon or a label instead of the slot root),
                    // you can log its parent tree:
                    VisualElement current = targetElement;
                    string path = current.name;
                    while (current.parent != null)
                    {
                        current = current.parent;
                        path = $"{current.name} -> {path}";
                    }
                    Debug.Log($"[Drag Test] Element Path: {path}");
                }
                else
                {
                    Debug.Log("[Drag Test] Picked Element was NULL (Dropped outside the UI or hit nothing).");
                }

                // Assuming the drop target might be a nested element (like an icon or label) inside the root
                // Traverse up to find the element containing our SlotDropData or a ProxySlotID.
                object targetData = FindTargetDropData(targetElement);

                if (targetData is SlotDropData targetSlotData)
                {
                    if (targetSlotData.Container != null && targetSlotData.Slot != null)
                    {
                        if (targetSlotData.Slot != _slotData || targetSlotData.Container != _owningContainer)
                        {
                            // Invoke the cross-container swap logic
                            _uiInventoryEvents?.OnRequestMoveItem?.Invoke(_owningContainer, _slotData, targetSlotData.Container, targetSlotData.Slot);
                            Debug.Log($"FIRING EVENT: Moving {_slotData.HeldItem.BaseItem.ItemName} to new slot.");
                        }
                    }
                }
                else if (targetData is string proxySlotID)
                {
                    // It's a proxy slot
                    _uiInventoryEvents?.OnRequestSelectForProcessing?.Invoke(_slotData, proxySlotID);
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

        private object FindTargetDropData(VisualElement element)
        {
            while (element != null)
            {
                if (element.userData is SlotDropData data)
                {
                    return data;
                }
                else if (element.userData is string proxySlotID)
                {
                    return proxySlotID;
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