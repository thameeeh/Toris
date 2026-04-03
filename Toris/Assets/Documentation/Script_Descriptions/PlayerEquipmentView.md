Identifier: OutlandHaven.Inventory.PlayerEquipmentView : IDisposable

Architectural Role: Component Logic / View Wrapper

Core Logic (The 'Contract'):
- Abstract/Virtual Methods: None.
- Public API:
  - Initialize(): Stub for initial setup.
  - Setup(InventoryManager equipmentInventory): Injects data dependency and refreshes slot visuals.
  - Show(): Subscribes to inventory events and refreshes visuals.
  - Hide(): Unsubscribes from inventory events.
  - Dispose(): Unsubscribes from events to prevent memory leaks during destruction.

Dependency Graph (Crucial for Scaling):
- Upstream: Requires UI Toolkit (VisualElement, VisualTreeAsset, TemplateContainer), UIInventoryEventsSO, InventoryManager, and InventorySlotView.
- Downstream: Instantiated and managed by PlayerInventoryView.

Data Schema:
- VisualElement _topElement -> Root container for the view.
- VisualTreeAsset _slotTemplate -> UXML template for equipment slots.
- UIInventoryEventsSO _uiInventoryEvents -> Global event bus for UI inventory changes.
- InventoryManager _equipmentInventory -> Injected data source containing equipment stats/slots.
- bool _eventsBound -> Tracks active event subscription state.
- VisualElements (_slotHeadContainer, _slotChestContainer, _slotLegsContainer, _slotArmsContainer, _slotWeaponContainer) -> Hardcoded DOM queries mapped to equipment indices.

Side Effects & Lifecycle:
- Uses manual initialization and manual visibility lifecycle (Setup, Show, Hide, Dispose) rather than MonoBehaviour lifecycle.
- Instantiates `VisualTreeAsset` into the DOM and allocates new `InventorySlotView` wrappers on the managed heap each time `RefreshSlots` is triggered (on `OnInventoryUpdated` or initial setup).
- Clears visual containers and applies direct inline styling (`style.display = DisplayStyle.None`) to "count-label" child elements.