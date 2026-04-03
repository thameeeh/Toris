# General Project Changelog

**Rules:**
* Archive previous changes and add new ones at the top to continue the log.
* Shortly describe what was done.
* Enumerate or mark different changes; if changes are too big, divide them into smaller ones.

---

## [Current/Recent] - Documentation Updates
This update addresses missing UI documentation and ensures all project documentation is centralized and correctly formatted according to project conventions.

### 1. Centralized Event Documentation
* Moved `Inventory_Event_System_Documentation.md` from the Scripts folder to the centralized `Toris/Assets/Documentation/` directory.

### 2. Added UI Interactions Documentation
* Created `UI_Interactions_Documentation.md` detailing the Drag-and-Drop system, Ghost Icon instantiation, Drag Thresholds, and the abstraction of raw hardware inputs into semantic events via `UIInventoryEventsSO`.

### 3. Added Equipment System Documentation
* Created `Equipment_System_Documentation.md` detailing the architecture of the Equipment UI and the stat connection flow (from `InventoryManager` via `PlayerEquipmentController` to `PlayerEffectResolver`).

### 4. Updated Script Dependencies
* Updated `script dependency documentation.md` to format relationships as proper dependency chains (A -> B -> C) rather than nested lists.
* Added cross-references to the newly created documentation files.

---

## [Previous] - Fixed Dynamic Inventory Growth Bug
This update fixes an issue where the `InventoryManager`'s live slot list would grow beyond the scriptable object's defined capacity when initialized with existing items in the Unity Editor or during gameplay, which caused the UI to break.

### 1. Updated Initialization Logic
* Modified `Awake()` in `InventoryManager.cs` to explicitly synchronize the `LiveSlots` count with the `ContainerBlueprint.SlotCount`. It now pads missing slots or trims excess ones, preventing the list from blindly appending slots.

### 2. Added Editor Validation
* Added an `OnValidate()` method wrapped in `#if UNITY_EDITOR` to `InventoryManager.cs`. This ensures that any manual changes in the Unity Inspector immediately reflect the correct, constrained slot count defined by the `ContainerBlueprint`.

---

## [Previous] - Refactor Player Data Architecture
This update refactors how global managers and the HUD access player progression and stats, removing the deprecated `PlayerDataSO` in favor of a Hybrid Architecture using Runtime Anchors and a UI Bridge.

### 1. Created Anchors
* Added `PlayerProgressionAnchorSO` and `PlayerStatsAnchorSO` ScriptableObjects to act as global access points.
* `PlayerProgression` and `PlayerStats` MonoBehaviours now register themselves to these anchors on `OnEnable` and clear on `OnDisable`.

### 2. Refactored Global Managers
* Updated `ShopManagerSO`, `CraftingManagerSO`, `SalvageManagerSO`, and `UpgradeSalvageManagerSO` to use `PlayerProgressionAnchorSO` for checking and deducting gold, removing their dependency on `PlayerDataSO`.

### 3. Updated HUD Controller
* Modified `HudScreenController` to find the `PlayerHUDBridge` in the scene and pass it to `HUDView` instead of `GameSessionSO.PlayerData`.
* `HUDView` now binds to the events of `PlayerHUDBridge` (`OnHealthChanged`, `OnStaminaChanged`, `OnLevelChanged`, `OnGoldChanged`) ensuring a decoupled, event-driven update loop.

### 4. Removed Deprecated Assets
* Deleted `PlayerDataSO.cs` entirely and cleaned up its references in `GameSessionSO` and `Wolf.cs`.

---

## [Previous] - USS and UXML Styling Cleanup
This update refactors the UI styling to consistently use global variables and BEM naming conventions across all UI Toolkit assets.

### 1. Updated Global Styles
* Added new CSS variables to `GlobalStyles.uss` for health (`--color-health`), mana (`--color-mana`), xp (`--color-xp`), and a dark panel background (`--color-panel-bg-dark`).

### 2. Refactored USS Files
* **Inventory.uss**: Replaced hardcoded background colors in Shop, Forge, and Salvage subviews with global CSS variables. Added a `.inventory-slot__icon--hidden` override class.
* **HUD.uss**: Converted ID selectors (e.g., `#hud__health-bar`) to BEM classes (e.g., `.hud-bar--health`) and removed empty `:root` blocks. Added layout classes to replace inline styles.
* **MainMenuButtons.uss & StatLabel.uss**: Renamed PascalCase classes to lowercase kebab-case (`.main-menu-btn`, `.stat-label`) to enforce BEM conventions. Removed empty `:root` blocks.
* **MasterLayout.uss**: Created a new stylesheet to hold the BEM layout classes for the main UI structure (`.master-layout`, `.master-layout__left-zone`, etc.).

### 3. Cleaned UXML Files
* Removed all inline `style="..."` attributes from `HUD.uxml`, `MasterLayout.uxml`, `MainMenu.uxml`, `HUDMenuButtonTemplate.uxml`, and `Slot.uxml`.
* Applied the newly defined BEM classes and layout classes to the elements.
* Assigned the `inventory-slot` class to the naked equipment visual elements in `PlayerInventory.uxml`.
* Added `<Style src="..." />` tags to sub-templates (`Slot.uxml`, `ShopSubView.uxml`, `HUDMenuButtonTemplate.uxml`) for accurate UI Builder previewing.

### 4. Updated C# Controllers
* **InventorySlotView.cs**: Modified the code to handle icon visibility by toggling the `.inventory-slot__icon--hidden` class instead of hardcoding `style.display = DisplayStyle.None;`.

---

## [Previous] - Drag-and-Drop functionality for Shop, Salvage, and Forge SubViews
This update implements drag-and-drop support for Shop, Salvage, and Forge UI subviews, ensuring consistency with the player inventory drag-and-drop system.

### 1. Fixed ShopSubView Initialization
* Updated `ShopSubView` to properly pass its `_shopContainer` and `_uiInventoryEvents` dependencies into the `InventorySlotView` constructor, enabling drag-and-drop functionality within the shop.

### 2. Added `OnRequestSelectForProcessing` Event
* Added `OnRequestSelectForProcessing` to `UIInventoryEventsSO` to handle drag-and-drop operations targeting proxy visual slots (like Salvage and Forge inputs) that do not have a backing `InventoryManager`.

### 3. Updated `InventorySlotView` Drop Logic
* Modified `InventorySlotView.OnPointerUp` to recognize proxy slots via string IDs stored in `VisualElement.userData`.
* When an item is dropped onto a proxy slot, it now invokes `OnRequestSelectForProcessing` instead of attempting a cross-container move.

### 4. Implemented Full Stack Drag-and-Drop in Salvage and Forge
* Updated `SalvageSubView` and `ForgeSubView` to assign string proxy IDs to their visual input slots (`salvage-input`, `forge-slot-1`, `forge-slot-2`).
* Subscribed both views to `OnRequestSelectForProcessing` to visually populate the proxy slots with the full stack count of the dragged item.
* Cached the original source `InventorySlot` from the player's inventory when an item is dropped or clicked into a proxy slot. This ensures that when the salvage or forge operation is executed, the actual player inventory slot is validated and consumed, preventing potential exploits where an item could be moved or sold before crafting.

---

## [Previous] - UI Toolkit Drag-and-Drop System
This update introduces a robust drag-and-drop mechanism for the inventory using Unity's UI Toolkit, complete with a drag threshold, a dedicated global overlay for dragging, and cross-container logic.

### 1. Updated Event Architecture
* Added `OnRequestMoveItem` to `UIInventoryEventsSO` to pass cross-container item transfer requests (source/target managers and slots).

### 2. Transitioned to Pointer Events
* Updated `InventorySlotView` to listen to `PointerDownEvent`, `PointerMoveEvent`, and `PointerUpEvent` instead of basic clicks.
* Implemented a 10px drag threshold. If the pointer moves less than this, it correctly falls back to firing the legacy `OnItemClicked` event.
* Added `SlotDropData` to `VisualElement.userData` to uniquely identify drop targets during raycast picking (`panel.Pick`).

### 3. Added Dedicated `UIDragManager`
* Created a clean, singleton `UIDragManager` component to isolate pointer tracking and visual drag state from `UIManager`.
* Programmatically injects a root `#Drag_Layer` and a `#Ghost_Icon` at runtime.
* Ensures the ghost icon has `picking-mode: ignore` so it does not block the drop target raycast.

### 4. Added Centralized `InventoryTransferManagerSO`
* Created a centralized, event-driven manager to handle logic between two distinct `InventoryManager` instances.
* Added logic to evaluate target slots for available space (partial stack merging), empty slots (direct moves), and mismatched items (item swaps).
* Fires `OnInventoryUpdated` on success.

### 5. Updated Views for Dependency Injection
* `PlayerInventoryView` and `PlayerEquipmentView` now pass the required `InventoryManager` and `UIInventoryEventsSO` dependencies directly into the `InventorySlotView` constructor to enable self-contained logic mapping.

---

## [Previous] - Equipment Click Interactions
This update implements click-to-equip and click-to-unequip functionality for the player's inventory, improving the usability of equipment management.

### 1. GameSessionSO Dependency Added
* Added a serialized reference to `GameSessionSO` (`_globalSession`) inside `PlayerEquipmentController`.
* **Reason:** This allows the controller to access the main player inventory (`_globalSession.PlayerInventory`) to verify if the clicked item belongs to the player's general storage.

### 2. UI Event Listeners Implemented
* Subscribed `PlayerEquipmentController` to the `_uiInventoryEvents.OnItemClicked` event.
* The system now listens for items that are clicked by the player anywhere in the UI that uses `InventorySlotView` components emitting this event.

### 3. Click-to-Equip Logic
* When an item in the main inventory is clicked, the system checks if its underlying `BaseItem` contains an `EquipableComponent`.
* If true, it extracts the `TargetSlot` enum from the component and attempts to move the item to the corresponding equipment slot (e.g., Head, Chest, Weapon).

### 4. Slot Swapping Mechanism
* When an equipment slot is already occupied, the logic now supports item swapping.
* Clicking on a new weapon in the inventory while a weapon is already equipped will seamlessly swap the two items, placing the old weapon back into the inventory slot that the new weapon just vacated.

### 5. Click-to-Unequip Logic
* When an item located inside an equipment slot (e.g., the currently equipped Weapon) is clicked, the system intercepts this and treats it as an unequip request.
* The system calls `_globalSession.PlayerInventory.AddItem(...)` to move the item back to the general inventory. If there is enough space, the equipment slot is cleared.

### Documentation Refactor
* Reorganized all documentation related to the UI, inventory, and item systems to ensure single-topic focus per document.
* Replaced `Inventory_Event_System_Documentation.md` with targeted documents: `Event_Architecture_Documentation.md` and `Inventory_Management_Documentation.md`.
* Renamed `Item_System_Architecture_Documentation.md` to `Item_Architecture_Documentation.md` and `UI_System_Documentation.md` to `UI_Architecture_Documentation.md` for naming consistency.
* Fixed typos in `General_Scripting_Conventions.md` pathing examples (e.g., `ScritableObjects` to `ScriptableObjects`).

## [Unreleased]
### Changed
- **UI Architecture:** Fixed broken drag-and-drop and click interactions on dynamically instantiated UI Toolkit inventory slots by updating the `TemplateContainer` wrapper's picking mode to `Ignore` and correctly registering pointer events directly onto the inner `.item-slot` element in `InventorySlotView.cs`.
# General Project Changelog

**Rules:**
* Archive previous changes and add new ones at the top to continue the log.
* Shortly describe what was done.
* Enumerate or mark different changes; if changes are too big, divide them into smaller ones.

---

## [Current/Recent] - Documentation Updates
This update introduces Context-Dense Metadata Summaries for critical UI components to aid AI-assisted development and architectural comprehension.

### 1. Created Script Descriptions
* Added `PlayerEquipmentView.md` in `Documentation/Script_Descriptions/` detailing its architecture, dependencies, and lifecycle.
* Added `PlayerInventoryView.md` in `Documentation/Script_Descriptions/` mapping its role as a screen controller, data dependencies, and state management.

---

## [Previous] - Fixed Dynamic Inventory Growth Bug
This update fixes an issue where the `InventoryManager`'s live slot list would grow beyond the scriptable object's defined capacity when initialized with existing items in the Unity Editor or during gameplay, which caused the UI to break.

### 1. Updated Initialization Logic
* Modified `Awake()` in `InventoryManager.cs` to explicitly synchronize the `LiveSlots` count with the `ContainerBlueprint.SlotCount`. It now pads missing slots or trims excess ones, preventing the list from blindly appending slots.

### 2. Added Editor Validation
* Added an `OnValidate()` method wrapped in `#if UNITY_EDITOR` to `InventoryManager.cs`. This ensures that any manual changes in the Unity Inspector immediately reflect the correct, constrained slot count defined by the `ContainerBlueprint`.

---

## [Previous] - Refactor Player Data Architecture
This update refactors how global managers and the HUD access player progression and stats, removing the deprecated `PlayerDataSO` in favor of a Hybrid Architecture using Runtime Anchors and a UI Bridge.

### 1. Created Anchors
* Added `PlayerProgressionAnchorSO` and `PlayerStatsAnchorSO` ScriptableObjects to act as global access points.
* `PlayerProgression` and `PlayerStats` MonoBehaviours now register themselves to these anchors on `OnEnable` and clear on `OnDisable`.

### 2. Refactored Global Managers
* Updated `ShopManagerSO`, `CraftingManagerSO`, `SalvageManagerSO`, and `UpgradeSalvageManagerSO` to use `PlayerProgressionAnchorSO` for checking and deducting gold, removing their dependency on `PlayerDataSO`.

### 3. Updated HUD Controller
* Modified `HudScreenController` to find the `PlayerHUDBridge` in the scene and pass it to `HUDView` instead of `GameSessionSO.PlayerData`.
* `HUDView` now binds to the events of `PlayerHUDBridge` (`OnHealthChanged`, `OnStaminaChanged`, `OnLevelChanged`, `OnGoldChanged`) ensuring a decoupled, event-driven update loop.

### 4. Removed Deprecated Assets
* Deleted `PlayerDataSO.cs` entirely and cleaned up its references in `GameSessionSO` and `Wolf.cs`.

---

## [Previous] - USS and UXML Styling Cleanup
This update refactors the UI styling to consistently use global variables and BEM naming conventions across all UI Toolkit assets.

### 1. Updated Global Styles
* Added new CSS variables to `GlobalStyles.uss` for health (`--color-health`), mana (`--color-mana`), xp (`--color-xp`), and a dark panel background (`--color-panel-bg-dark`).

### 2. Refactored USS Files
* **Inventory.uss**: Replaced hardcoded background colors in Shop, Forge, and Salvage subviews with global CSS variables. Added a `.inventory-slot__icon--hidden` override class.
* **HUD.uss**: Converted ID selectors (e.g., `#hud__health-bar`) to BEM classes (e.g., `.hud-bar--health`) and removed empty `:root` blocks. Added layout classes to replace inline styles.
* **MainMenuButtons.uss & StatLabel.uss**: Renamed PascalCase classes to lowercase kebab-case (`.main-menu-btn`, `.stat-label`) to enforce BEM conventions. Removed empty `:root` blocks.
* **MasterLayout.uss**: Created a new stylesheet to hold the BEM layout classes for the main UI structure (`.master-layout`, `.master-layout__left-zone`, etc.).

### 3. Cleaned UXML Files
* Removed all inline `style="..."` attributes from `HUD.uxml`, `MasterLayout.uxml`, `MainMenu.uxml`, `HUDMenuButtonTemplate.uxml`, and `Slot.uxml`.
* Applied the newly defined BEM classes and layout classes to the elements.
* Assigned the `inventory-slot` class to the naked equipment visual elements in `PlayerInventory.uxml`.
* Added `<Style src="..." />` tags to sub-templates (`Slot.uxml`, `ShopSubView.uxml`, `HUDMenuButtonTemplate.uxml`) for accurate UI Builder previewing.

### 4. Updated C# Controllers
* **InventorySlotView.cs**: Modified the code to handle icon visibility by toggling the `.inventory-slot__icon--hidden` class instead of hardcoding `style.display = DisplayStyle.None;`.

---

## [Previous] - Drag-and-Drop functionality for Shop, Salvage, and Forge SubViews
This update implements drag-and-drop support for Shop, Salvage, and Forge UI subviews, ensuring consistency with the player inventory drag-and-drop system.

### 1. Fixed ShopSubView Initialization
* Updated `ShopSubView` to properly pass its `_shopContainer` and `_uiInventoryEvents` dependencies into the `InventorySlotView` constructor, enabling drag-and-drop functionality within the shop.

### 2. Added `OnRequestSelectForProcessing` Event
* Added `OnRequestSelectForProcessing` to `UIInventoryEventsSO` to handle drag-and-drop operations targeting proxy visual slots (like Salvage and Forge inputs) that do not have a backing `InventoryManager`.

### 3. Updated `InventorySlotView` Drop Logic
* Modified `InventorySlotView.OnPointerUp` to recognize proxy slots via string IDs stored in `VisualElement.userData`.
* When an item is dropped onto a proxy slot, it now invokes `OnRequestSelectForProcessing` instead of attempting a cross-container move.

### 4. Implemented Full Stack Drag-and-Drop in Salvage and Forge
* Updated `SalvageSubView` and `ForgeSubView` to assign string proxy IDs to their visual input slots (`salvage-input`, `forge-slot-1`, `forge-slot-2`).
* Subscribed both views to `OnRequestSelectForProcessing` to visually populate the proxy slots with the full stack count of the dragged item.
* Cached the original source `InventorySlot` from the player's inventory when an item is dropped or clicked into a proxy slot. This ensures that when the salvage or forge operation is executed, the actual player inventory slot is validated and consumed, preventing potential exploits where an item could be moved or sold before crafting.

---

## [Previous] - UI Toolkit Drag-and-Drop System
This update introduces a robust drag-and-drop mechanism for the inventory using Unity's UI Toolkit, complete with a drag threshold, a dedicated global overlay for dragging, and cross-container logic.

### 1. Updated Event Architecture
* Added `OnRequestMoveItem` to `UIInventoryEventsSO` to pass cross-container item transfer requests (source/target managers and slots).

### 2. Transitioned to Pointer Events
* Updated `InventorySlotView` to listen to `PointerDownEvent`, `PointerMoveEvent`, and `PointerUpEvent` instead of basic clicks.
* Implemented a 10px drag threshold. If the pointer moves less than this, it correctly falls back to firing the legacy `OnItemClicked` event.
* Added `SlotDropData` to `VisualElement.userData` to uniquely identify drop targets during raycast picking (`panel.Pick`).

### 3. Added Dedicated `UIDragManager`
* Created a clean, singleton `UIDragManager` component to isolate pointer tracking and visual drag state from `UIManager`.
* Programmatically injects a root `#Drag_Layer` and a `#Ghost_Icon` at runtime.
* Ensures the ghost icon has `picking-mode: ignore` so it does not block the drop target raycast.

### 4. Added Centralized `InventoryTransferManagerSO`
* Created a centralized, event-driven manager to handle logic between two distinct `InventoryManager` instances.
* Added logic to evaluate target slots for available space (partial stack merging), empty slots (direct moves), and mismatched items (item swaps).
* Fires `OnInventoryUpdated` on success.

### 5. Updated Views for Dependency Injection
* `PlayerInventoryView` and `PlayerEquipmentView` now pass the required `InventoryManager` and `UIInventoryEventsSO` dependencies directly into the `InventorySlotView` constructor to enable self-contained logic mapping.

---

## [Previous] - Equipment Click Interactions
This update implements click-to-equip and click-to-unequip functionality for the player's inventory, improving the usability of equipment management.

### 1. GameSessionSO Dependency Added
* Added a serialized reference to `GameSessionSO` (`_globalSession`) inside `PlayerEquipmentController`.
* **Reason:** This allows the controller to access the main player inventory (`_globalSession.PlayerInventory`) to verify if the clicked item belongs to the player's general storage.

### 2. UI Event Listeners Implemented
* Subscribed `PlayerEquipmentController` to the `_uiInventoryEvents.OnItemClicked` event.
* The system now listens for items that are clicked by the player anywhere in the UI that uses `InventorySlotView` components emitting this event.

### 3. Click-to-Equip Logic
* When an item in the main inventory is clicked, the system checks if its underlying `BaseItem` contains an `EquipableComponent`.
* If true, it extracts the `TargetSlot` enum from the component and attempts to move the item to the corresponding equipment slot (e.g., Head, Chest, Weapon).

### 4. Slot Swapping Mechanism
* When an equipment slot is already occupied, the logic now supports item swapping.
* Clicking on a new weapon in the inventory while a weapon is already equipped will seamlessly swap the two items, placing the old weapon back into the inventory slot that the new weapon just vacated.

### 5. Click-to-Unequip Logic
* When an item located inside an equipment slot (e.g., the currently equipped Weapon) is clicked, the system intercepts this and treats it as an unequip request.
* The system calls `_globalSession.PlayerInventory.AddItem(...)` to move the item back to the general inventory. If there is enough space, the equipment slot is cleared.

### Documentation Refactor
* Reorganized all documentation related to the UI, inventory, and item systems to ensure single-topic focus per document.
* Replaced `Inventory_Event_System_Documentation.md` with targeted documents: `Event_Architecture_Documentation.md` and `Inventory_Management_Documentation.md`.
* Renamed `Item_System_Architecture_Documentation.md` to `Item_Architecture_Documentation.md` and `UI_System_Documentation.md` to `UI_Architecture_Documentation.md` for naming consistency.
* Fixed typos in `General_Scripting_Conventions.md` pathing examples (e.g., `ScritableObjects` to `ScriptableObjects`).

## [Unreleased]
### Changed
- **UI Architecture:** Fixed vertical scrollbar bug in the player inventory UI (`PlayerInventory.uxml`) by forcing the `vertical-scroller-visibility` to `Hidden`, correctly addressing the layout calculation bug on subsequent openings for the fixed 21-slot grid.
- **UI Architecture:** Removed hardcoded dummy slot instances from `PlayerInventory.uxml` grid, ensuring a purely data-driven template instantiation approach in compliance with project architectural directives.
- **UI Architecture:** Refactored the UI Toolkit inventory assets (`PlayerInventory`, `Mage`, `Smith`, `ShopSubView`, `ForgeSubView_Smith`, `SalvageSubView_Smith`, `Slot`) to strictly follow BEM naming conventions.
- **UI Architecture:** Extracted all inline UXML styles into a new `GlobalStyles.uss` file using `:root` CSS variables and applied them correctly across the project in `Inventory.uss`.
- **UI Controllers:** Updated C# View controllers (`InventorySlotView`, `PlayerEquipmentView`) to query the newly named BEM classes, preventing runtime query failures.
