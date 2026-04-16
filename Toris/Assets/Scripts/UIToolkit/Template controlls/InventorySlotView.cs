using System;
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
        public event Action<InventorySlot> OnLocalClicked;
        public event Action<InventorySlot> OnLocalRightClicked;
        public event Action<InventoryManager, InventorySlot, InventoryManager, InventorySlot, int> OnLocalMoveItemRequested;
        public event Action<InventorySlot, string> OnLocalSelectForProcessingRequested;

        public event Action<Sprite, Vector2, Vector2> OnLocalDragStarted;
        public event Action<Vector2> OnLocalDragUpdated;
        public event Action OnLocalDragStopped;

        // Drag and Drop State
        private bool _isDragging = false;
        private Vector2 _dragStartPosition;
        private const float DragThreshold = 10f; // Pixels to move before initiating drag
        private int _dragAmount = 0;

        public InventorySlotView(VisualElement root, InventoryManager owningContainer)
        {
            // Set the wrapper root's picking mode to Ignore
            root.pickingMode = PickingMode.Ignore;

            _root = root.Q<VisualElement>(className: "item-slot");
            if (_root == null) _root = root;
            else _root.pickingMode = PickingMode.Position;

            _owningContainer = owningContainer;


            _icon = _root.Q<Image>("slot-icon");
            _qtyLabel = _root.Q<Label>("slot-qty");

            // FACTUAL FIX 1: Force child elements to ignore raycasts so _root catches the click cleanly
            if (_icon != null) _icon.pickingMode = PickingMode.Ignore;
            if (_qtyLabel != null) _qtyLabel.pickingMode = PickingMode.Ignore;

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
                _qtyLabel.text = GetQuantityText(slotData);
            }
        }

        private static string GetQuantityText(InventorySlot slotData)
        {
            if (slotData == null || slotData.IsEmpty)
                return string.Empty;

            if (slotData.Count > 1)
                return slotData.Count.ToString();

            ConsumableState consumableState = slotData.HeldItem?.GetState<ConsumableState>();
            if (consumableState != null && consumableState.CurrentCharges > 1)
                return consumableState.CurrentCharges.ToString();

            return string.Empty;
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

            // FACTUAL FIX 2: evt.button is unreliable during a move. 
            // Check the evt.pressedButtons bitmask instead. (1 = Left Click held down)
            if ((evt.pressedButtons & 1) == 0) return;

            if (!_isDragging)
            {
                float distance = Vector2.Distance(_dragStartPosition, evt.position);
                if (distance >= DragThreshold)
                {
                    _isDragging = true;
                    _dragAmount = evt.shiftKey ? Mathf.CeilToInt(_slotData.Count / 2f) : _slotData.Count;

                    float width = _icon.resolvedStyle.width;
                    float height = _icon.resolvedStyle.height;

                    // Failsafe: If UI Toolkit hasn't resolved the flexbox math yet, default to a standard icon size (e.g., 50x50)
                    if (float.IsNaN(width) || width <= 0) width = 80f;
                    if (float.IsNaN(height) || height <= 0) height = 80f;

                    Vector2 iconSize = new Vector2(width, height);
                    OnLocalDragStarted?.Invoke(_slotData.HeldItem.BaseItem.Icon, evt.position, iconSize);
                }
            }
            else
            {
                OnLocalDragUpdated?.Invoke(evt.position);
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
                OnLocalDragStopped?.Invoke();

                // Fire right click event
                if (_slotData != null && !_slotData.IsEmpty)
                {
                    OnLocalRightClicked?.Invoke(_slotData);
                }
                return;
            }

            if (_isDragging)
            {
                // Stop dragging state and clear visual ghost
                _isDragging = false;
                OnLocalDragStopped?.Invoke();

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
                            OnLocalMoveItemRequested?.Invoke(_owningContainer, _slotData, targetSlotData.Container, targetSlotData.Slot, _dragAmount);
                            Debug.Log($"FIRING EVENT: Moving {_slotData.HeldItem.BaseItem.ItemName} to new slot.");
                        }
                    }
                }
                else if (targetData is string proxySlotID)
                {
                    // It's a proxy slot
                    OnLocalSelectForProcessingRequested?.Invoke(_slotData, proxySlotID);
                }
            }
            else
            {
                // Pointer did not move past the threshold, treat as a normal click
                if (evt.button == 0 && _slotData != null && !_slotData.IsEmpty)
                {
                    OnLocalClicked?.Invoke(_slotData);
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

        public void Dispose()
        {
            if (_root != null)
            {
                _root.UnregisterCallback<PointerDownEvent>(OnPointerDown);
                _root.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
                _root.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            }
        }
    }

    public class SlotDropData
    {
        public InventorySlot Slot { get; set; }
        public InventoryManager Container { get; set; }
    }
}
