Identifier: OutlandHaven.UIToolkit.ShopSubView : UIView

Architectural Role: Component Logic / Contextual UI SubView

Core Logic (The 'Contract'):
- Abstract/Virtual Methods: Overrides `SetVisualElements()`, `Setup(object)`, `Show()`, `Hide()`, `Dispose()`.
- Public API:
  - `Setup(object payload)`: Expects `InventoryManager` payload. Binds shop container, recreates slots, and updates gold display.
  - `Show()`: Binds global events.
  - `Hide()`: Unbinds global events.
  - `Dispose()`: Unbinds remaining events and calls base `Dispose()`.

Dependency Graph (Crucial for Scaling):
- Upstream: Depends on `InventoryManager` (dynamic shop data), `VisualTreeAsset` (slot template), `UIInventoryEventsSO` (event channels), `GameSessionSO` (player inventory check), `PlayerHUDBridge` (player gold check).
- Downstream: Instantiated and managed by `SmithView`. (Potentially other merchant views).

Data Schema:
- `InventoryManager _shopContainer`: Reference to the active shop's inventory.
- `PlayerHUDBridge _playerHudBridge`: Reference to read player gold.
- `List<InventorySlotView> _slotViews`: Tracks instantiated slot views for lifecycle management and disposal.
- `const int BULK_BUY_AMOUNT = 10`: Hardcoded bulk transaction amount.

Side Effects & Lifecycle:
- Clears and rebuilds the `_shopGrid` dynamically during `Setup()` or when `OnShopInventoryUpdated` is fired. Instantiates `TemplateContainer` objects for every slot.
- Disposes previous `InventorySlotView` instances before rebuilding to prevent memory leaks.
- Subscribes to global events (``, `OnShopInventoryUpdated`, `OnItemRightClicked`) upon `Show()`.
- Broadcasts transaction requests (`OnRequestBuy`, `OnRequestSell`) via `UIInventoryEventsSO` when items are right-clicked, depending on which container the item belongs to.
- Does not use Unity `Update()` loop.
