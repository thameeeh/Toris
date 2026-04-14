- **Identifier:** `OutlandHaven.UIToolkit.MageView : GameView`
- **Architectural Role:** Context Wrapper / Aggregation View for Mage NPC interactions.

- **Core Logic (The 'Contract'):**
  - **Abstract/Virtual Methods:**
    - `ID` (Property): Implements `ScreenType.Mage`.
    - `SetVisualElements()`: Overridden to bind the middle panel container and market tab button.
    - `Setup(object payload)`: Overridden to cast payload into dynamic `InventoryManager` (shop container) and initialize the default active tab.
    - `Hide()`: Overridden to cascade `Hide()` calls to nested sub-views.
    - `Dispose()`: Overridden to cascade `Dispose()` calls to nested sub-views.
  - **Public API:**
    - `MageView(...)`: Constructor for heavy dependency injection (Templates, Events, Session, ShopManager).

- **Dependency Graph (Crucial for Scaling):**
  - **Upstream:**
    - Requires `OutlandHaven.UIToolkit.GameView` (Base Class).
    - Requires `OutlandHaven.UIToolkit.ShopSubView` (Nested logic view).
    - Requires `OutlandHaven.Inventory.InventoryManager` (Data container for dynamic shop inventory).
    - Requires `UnityEngine.UIElements.VisualTreeAsset` (Templates for slot and shop structure).
  - **Downstream:**
    - Handled by global `UIManager` via specific NPC interaction events.

- **Data Schema:**
  - `_slotTemplate`, `_shopTemplate` (VisualTreeAsset): UI structure templates.
  - `_shopContainer` (InventoryManager): Caches the dynamic payload representing the NPC's inventory state.
  - `_shopSubView` (ShopSubView): Managed instance of the specialized shop logic controller.

- **Side Effects & Lifecycle:**
  - **Lifecycle:** Standard manual initialization lifecycle driven by `UIManager`. Sub-view lifecycle (`Initialize`, `Setup`, `Show`, `Hide`, `Dispose`) is explicitly orchestrated within this class.
  - **Side Effects:** Employs 'Lazy Initialization'—instantiating and binding the `ShopSubView` UI elements into `_middlePanel` only when `ShowMarketTab()` is first requested. Binds a click listener to the `Mage_Market--Tab` UI element.
