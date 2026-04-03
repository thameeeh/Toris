Identifier: OutlandHaven.Inventory.PlayerInventoryView : GameView, IDisposable

Architectural Role: Component Logic / UI Screen Controller

Core Logic (The 'Contract'):
- Abstract/Virtual Methods: Inherits from GameView. Overrides ID (ScreenType.Inventory), Initialize, Show, Hide, SetVisualElements, and Setup.
- Public API:
  - Initialize(): Sets up base logic and instantiates the nested PlayerEquipmentView.
  - Setup(object payload): Refreshes the player inventory grid and passes equipment data to the PlayerEquipmentView.
  - Show(): Binds inventory update events and cascades the Show command to the equipment view.
  - Hide(): Unbinds inventory update events and cascades the Hide command to the equipment view.
  - Dispose(): Event cleanup for itself and cascaded Dispose for the equipment view.

Dependency Graph (Crucial for Scaling):
- Upstream: Requires GameView, GameSessionSO, UIEventsSO, UIInventoryEventsSO, InventoryManager, PlayerEquipmentView, and InventorySlotView.
- Downstream: Instantiated and managed by UIManager or screen arbitrators.

Data Schema:
- ScreenType ID -> Hardcoded to ScreenType.Inventory.
- VisualTreeAsset _slotTemplate -> Template for individual inventory slots.
- GameSessionSO _gameSession -> Global source of truth for the primary PlayerInventory data.
- InventoryManager _equipmentInventory -> Distinct InventoryManager specifically for equipment.
- UIInventoryEventsSO _uiInventoryEvents -> Local event bus.
- VisualElement _playerGrid -> Target DOM container for standard inventory items.
- PlayerEquipmentView _equipmentView -> Encapsulated logic wrapper for the player's equipment UI.
- bool _eventsBound -> State flag for active event subscriptions.

Side Effects & Lifecycle:
- Lifecycle follows the standard `GameView` pattern (Initialize -> Setup -> Show/Hide -> Dispose).
- Instantiates `VisualTreeAsset` clones into the DOM and allocates new `InventorySlotView` wrappers on the managed heap each time `RefreshGrid` is triggered (during Setup or OnInventoryUpdated).
- Clears the entire visual hierarchy of `_playerGrid` before repopulating on refresh.