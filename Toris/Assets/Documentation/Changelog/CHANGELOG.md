## [Current/Recent] - Reorganize MapGeneration into clearer runtime folders
This update reshapes the MapGeneration script tree into a smaller set of human-readable folders so generation, runtime, sites, streaming, navigation, and diagnostics are easier to navigate without changing intended gameplay behavior.

### 1. Reorganized The Top-Level Structure
* Replaced the old mixed Extras, Interactable, POIs, Refactor, and WorldGen split with Diagnostics, Generation, Navigation, Runtime, Sites, and Streaming.

### 2. Kept Concrete Content Grouped
* Kept gate content under Sites/Gate and wolf-den content under Sites/WolfDen so authored site logic and prefabs remain easy to find.

### 3. Matched Shared Systems To Their Real Responsibilities
* Grouped build steps, generated output, tile generation, transitions, runtime site plumbing, nav systems, and streaming systems under folders that reflect what they actually own now.

### 4. Updated Documentation To Match
* Updated the world generation architecture guide to reference the new folder layout.
* Verified startup, chunk streaming, den behavior, persistence, biome gates, run gates, and the debug HUD after the reorganization.

---## [Current/Recent] - Conservative dead-code cleanup in MapGeneration
This update removes dead helper surfaces, stale commented scaffolding, and unused lifecycle bookkeeping from the map-generation stack without changing intended runtime behavior.

### 1. Removed Unused Lifecycle Bookkeeping
* Deleted ActiveSiteHandle and simplified chunk-site activation tracking to store active counts directly instead of carrying dead handle objects.
* Removed unused stored dependencies from lifecycle classes that were no longer read.

### 2. Removed Dead Helper Surface
* Removed unused APIs from pooled site ownership, POI pooling, home-anchor, and streaming runtime classes where no callers remained.
* Updated the world runner to match the simplified lifecycle construction path.

### 3. Removed Stale Commented Scaffolding
* Deleted obsolete commented debug lines, placeholder comments, and dead TODO-style notes that no longer reflected the current architecture.

### 4. Preserved Intentional Migration Paths
* Kept the wolf encounter legacy migration fields and policy transfer code because the live config asset still serializes those values.
* Verified startup, chunk streaming, den behavior, persistence, biome gates, run gates, and the debug HUD after the cleanup.

---## [Current/Recent] - Add World Generation Architecture Guide
This update adds a dedicated documentation guide for the current map-generation stack so future feature work can follow the live architecture instead of relying on refactor history alone.

### 1. Documented The Current Runtime World Stack
* Added a structured guide covering build output, streaming, lifecycle, activation, navigation, transitions, diagnostics, and the wolf-den reference implementation.

### 2. Documented The Extension Path For New World Content
* Added step-by-step instructions for passive structures, persistent structures, encounter structures, and new supporting systems.

### 3. Captured The Architectural Rules For Future Additions
* Documented the intended seams and anti-patterns so new world features can be added surgically instead of reopening monolithic runtime code.

---## [Current/Recent] - Decouple The World Debug HUD From The Runner
This update removes the world debug HUD's direct dependency on WorldGenRunner and replaces it with a narrow diagnostics contract.

### 1. Added A Dedicated Diagnostics Interface
* Added IWorldDiagnosticsSource so debug consumers read only the world context and diagnostics snapshot they actually need.

### 2. Rewired The HUD To The Narrow Boundary
* Updated WorldGenDebugHUD to use a serialized diagnostics source component through the new interface.
* Preserved existing scene serialization with FormerlySerializedAs("runner").

### 3. Preserved Existing Debug Behavior
* Verified compact HUD behavior, advanced diagnostics toggle, chunk visuals, biome transitions, and run gates after the change.

---
## [Current/Recent] - Delete Legacy Build-Path Compatibility Surface
This update removes old world-build compatibility aliases and a dead persistent profile field now that build output is the authoritative generated-world path.

### 1. Removed Old WorldContext Build Aliases
* The world context no longer exposes duplicate aliases for generated stamps, blockers, anchors, placements, or site registration.

### 2. Kept Lifecycle Readers On The Authoritative Build Path
* Site lifecycle rebuild paths now read placement data directly from WorldBuildOutput.

### 3. Removed Dead Persistent Profile Authoring Data
* The obsolete persistent-site field was removed from BiomeProfile after persistent site authoring moved into build-step assets.

---
## [Current/Recent] - Debug HUD Readability Trim
This update simplifies the default world debug HUD so it stays readable at larger font sizes while keeping deeper diagnostics available behind an advanced toggle.

### 1. Simplified The Default HUD
* The default HUD now focuses on visual toggles, biome, tile position, and the core sampled world signals.

### 2. Kept Deeper Diagnostics Available On Demand
* Grouped subsystem diagnostics remain available through an in-HUD advanced-stats toggle instead of crowding the default layout.

### 3. Preserved Existing Debug Utility
* Verified compact HUD readability and the continued behavior of chunk-border and streaming-rectangle toggles.

---
## [Current/Recent] - Grouped World Diagnostics Snapshots
This update restructures world diagnostics into grouped subsystem snapshots so the debug HUD reads from intentional read models instead of one large flat payload.

### 1. Added Subsystem Diagnostics Shapes
* Added separate snapshot types for streaming, lifecycle, navigation, and transitions.

### 2. Simplified The Aggregate Diagnostics Model
* WorldGenDiagnosticsSnapshot now groups subsystem snapshots instead of exposing every field directly.

### 3. Preserved Existing Debug Behavior
* Verified the HUD, chunk streaming, biome transitions, run gates, and den behavior after the diagnostics-model refactor.

---
## [Current/Recent] - World Streaming Runtime Extraction
This update moves the per-frame world streaming manager responsibilities into a dedicated runtime object so the world runner stays closer to a thin orchestration shell.

### 1. Added WorldStreamingRuntime
* Streaming camera resolution, frame settings, coordinator invocation, last-frame cache ownership, reset behavior, and warning logging now live in WorldStreamingRuntime.

### 2. Reduced WorldGenRunner Streaming Ownership
* WorldGenRunner.Update() now delegates streaming work through the runtime instead of constructing and managing the full streaming path itself.

### 3. Preserved Existing World Behavior
* Verified world startup, chunk streaming, debug HUD streaming data, biome transitions, run gates, and wolf den behavior after the extraction.

---
# General Project Changelog

**Rules:**
* Archive previous changes and add new ones at the top to continue the log.
* Shortly describe what was done.
* Enumerate or mark different changes; if changes are too big, divide them into smaller ones.

---

## [Current/Recent] - Drag-and-Drop functionality for Shop, Salvage, and Forge SubViews
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








