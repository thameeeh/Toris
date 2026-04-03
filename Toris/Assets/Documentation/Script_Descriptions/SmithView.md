Identifier: OutlandHaven.UIToolkit.SmithView : GameView

Architectural Role: Singleton Manager / Screen Controller (Mediator)

Core Logic (The 'Contract'):
- Abstract/Virtual Methods: Overrides `ScreenType ID` (returns `ScreenType.Smith`), `SetVisualElements()`, `Setup(object)`, `Hide()`, `Dispose()`.
- Public API:
  - `Setup(object payload)`: Expects `InventoryManager` payload for the shop. Forwards to subviews and defaults to showing the Market tab.

Dependency Graph (Crucial for Scaling):
- Upstream: Depends on `GameView` base class, multiple `VisualTreeAsset` templates (slot, shop, forge, salvage), `UIEventsSO`, `UIInventoryEventsSO`, `GameSessionSO`, `PlayerProgressionAnchorSO`, `CraftingManagerSO`, `SalvageManagerSO`.
- Downstream: Instantiated and managed by `UIManager` (or registered dynamically).

Data Schema:
- `InventoryManager _shopContainer`: Passed down to `ShopSubView`.
- SubView Instances: `_shopSubView`, `_forgeSubView`, `_salvageSubView`.
- Visual Elements: Tab buttons (`Smith_Market--Tab`, etc.) and `_middlePanel` container.

Side Effects & Lifecycle:
- Employs lazy initialization for SubViews (`ShopSubView`, `ForgeSubView`, `SalvageSubView`). They are only instantiated and added to the UI hierarchy when their respective tab is clicked for the first time.
- Manages subview visibility (`Show()`, `Hide()`) manually in response to tab clicks.
- Passes dependencies (Managers, SOs, Templates) down to the SubViews upon their initialization.
- Cascades `Dispose()` calls to all active SubViews when the SmithView is destroyed.
- Registers standard UI click callbacks for tabs. No Unity `Update()` loop used.
