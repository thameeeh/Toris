## [Current/Recent] - GameSessionSO Architectural Refactor
* **Code Decoupling:** Refactored the monolithic `GameSessionSO` into a Facade that delegates specialized tasks to new service classes:
    * `RuntimeSnapshotRegistry.cs`: Manages volatile in-memory state for scene transitions.
    * `SaveDataOrchestrator.cs`: Handles persistence logic, inventory extraction, and DTO mapping for save/load operations.
* **Documentation:** Added a stylized architectural overview header to `GameSessionSO` explaining the service roles.
* **Backward Compatibility:** Maintained all existing public method signatures to ensure zero breaking changes for UI and gameplay systems.

## [Current/Recent] - Item Module Serialization Compliance
* **Serialization Fixes:** Added mandatory public parameterless constructors to `UpgradeableState` in `UpgradeableModule.cs` to comply with `Newtonsoft.Json` requirements for save/load stability. (Note: `EvolvingState` was verified and already contains a parameterless constructor).
* **Verification Tools:** Created `SerializationSanityCheck.cs` in `Assets/Scripts/Debugging/` to provide a runtime context-menu test for validating `ItemComponentState` serialization.

## [Current/Recent] - Fix Potion and Equipment Inventory Scene Loading
* **Persistent Inventory Snapshots:** Refactored `GameSessionSO` to prevent clearing inventory snapshots after they are applied. This ensures inventory data remains available across multiple scene transitions and re-instantiations.
* **Potion Inventory Integration:** Added full support for the Potion inventory to the scene transition and save systems.
    * **Session Tracking:** Added `PlayerPotionInventory` and `_potionInventorySnapshot` to `GameSessionSO`.
    * **Capture/Restore:** Implemented `CapturePotionInventoryState` and `TryApplyPotionInventoryState` in `GameSessionSO`.
    * **Manager Binding:** Updated `InventoryManager` to automatically register and restore potion inventory state via `ScreenType.Potions` association.
    * **Persistence:** Added `PlayerPotion` to `GameSaveData` and updated export/import logic in `GameSessionSO` to include potion data.

## [Current/Recent] - Potion Hotkey Integration
* **Hotkey Consumption:** Implemented hotkey-driven potion consumption ('1' and '2') by wiring `PlayerInputReaderSO` to `InventoryActionController`, allowing direct item usage from the Potion quickbar slots.
* **Resolution Resolver:** Added `ResolvePotionInventory` to `PlayerInventorySceneResolver` to ensure correct runtime reference handling for potion containers during controller initialization.
* **UI/Inventory Fixes:** Added diagnostic logging in `InventoryActionController` for consumption events.

## [Current/Recent] - Missing Asset Safety
* **OnValidate Implementation:** Added `OnValidate` checks to `HudScreenController` and `InventoryActionController` to log warnings for missing references at design time (in the Unity Editor), preventing silent failures at runtime.
* **Lifecycle Cleanup:** Moved null reference checks from runtime lifecycle methods like `OnEnable` into `OnValidate` where appropriate to optimize runtime execution and provide earlier developer feedback.

## [Current/Recent] - GDD Finalization & Scope Alignment
* **GDD Finalization:** Updated `Toris/README.md` to precisely reflect the current Hub-and-Expedition loop and item-based economy.
* **Economic Reality Check:** Updated documentation to distinguish between abstract currencies (Gold/XP) and physical inventory resources (Food/Materials).
* **Scope Refinement:** Formally deprecated town-building references to ensure project documentation matches the implementation timeline.

## [Current/Recent] - GDD Update & Scope Refinement
* **GDD Overhaul:** Updated `Toris/README.md` to reflect the removal of the town-building system.
* **Scope Realignment:** Refocused the project's core loop on **Hub-and-Expedition** dynamics, emphasizing exploration, combat, and gear progression over infrastructure management.
* **Economic Simplification:** Streamlined the resource descriptions to focus on Gold, Materials, and XP, aligning with the current implementation.

## [Current/Recent] - Inventory Stability & UI Polish
* **Drag-and-Drop Fix:** Resolved a critical bug where dynamically instantiated inventory slots would lose pointer interaction.
    * **Template Isolation:** Explicitly set `pickingMode = PickingMode.Ignore` on all `TemplateContainer` wrappers in `PlayerInventoryView`, `PlayerEquipmentView`, `PlayerPotionView`, and `Smith` sub-views.
    * **Event Reliability:** Verified that `InventorySlotView.cs` correctly captures `PointerUpEvent` and resolves drop targets using `panel.Pick()`.
* **UI Toolkit Consistency:** 
    * **Flexbox Sizing:** Confirmed `flex-shrink: 0` is applied to all `.item-slot` and `.potion-slot` classes in `Components.uss` to prevent layout squishing.
    * **Theme Variables:** Verified that inventory stylesheets adhere to the `theme-variables.uss` system.

## [Current/Recent] - Feature: Save Deletion Confirmation & Reusable Modal
* **Generic Confirmation System:** Implemented a reusable `ConfirmationModal` architecture using a `ConfirmationPayload` DTO, allowing any system to request user validation before executing destructive actions.
* **Deletion Protection:** Integrated the confirmation modal into `MainMenuController.cs`, requiring explicit user consent before deleting save slots.
* **Application Exit Safety:** Added confirmation prompt when clicking the "Exit" button in the Main Menu to prevent accidental closure.
* **Architecture:** Followed strict MVP patterns with `ConfirmationModalView` (visuals) and `ConfirmationModalController` (logic/routing).

## [Current/Recent] - UI Polish & In-Game Pause Menu Implementation
* **In-Game Pause Menu:** Created a new MVP-based Pause Menu system.
    * **View:** `PauseMenuView.cs` handles visual layout and emits semantic events for Resume, Settings, and Main Menu.
    * **Controller:** `PauseMenuController.cs` manages `Time.timeScale`, input map switching (Player vs. UI), and scene transitions.
    * **Assets:** Created `PauseMenu.uxml` and `PauseMenu.uss` with a semi-transparent overlay and themed button group.
* **Settings Menu QoL:** Updated `SettingsMenuController.cs` to support closing the modal via the Escape key (mapped to `UI/Cancel` in the new Input System).
* **Save Slot Redesign:**
    * **Layout:** Repositioned the delete button to the bottom of the Save Slot card for better visual balance.
    * **Textual UI:** Replaced the "×" symbol with the word "Delete" as requested, styled to match the game's theme.
* **Event-Driven Quick Save/Load:** 
    * **Decoupling:** Migrated legacy `Input.GetKeyDown` logic in `SaveManager.cs` to a decoupled event-driven architecture.
    * **Event Bus:** Added `OnQuickSaveRequested` and `OnQuickLoadRequested` to `UIEventsSO.cs`.
    * **Input Bridging:** `InputManager.cs` now bridges the Unity Input System callbacks to these global UI events.

## [Current/Recent] - Feature: Save Deletion from Main Menu
* **Delete Interaction:** Added a red "×" delete button to each Save Slot card. It is styled using BEM conventions (`.save-slot__delete-btn`) and positioned absolutely for a clean UI overlay.
* **Event Propagation Safety:** Implemented `evt.StopImmediatePropagation()` in `SaveSlotView.cs` to ensure that clicking the delete button does not accidentally trigger the slot's loading logic.
* **File System Logic:** Added `DeleteSave` to `SaveManager.cs`, which physically removes the `.json` file from the disk and handles the cleanup of Slot 1's quicksave fallback.
* **UI Refresh:** Integrated the delete intent into `MainMenuController.cs`, ensuring the save slot list is instantly repopulated and refreshed after a deletion.

## [Current/Recent] - Critical Fixes: Save Restoration & Serialization
* **Automatic Data Restoration:** Fixed the "F9 required" issue by implementing inventory snapshots in `GameSessionSO`. Item data is now buffered during Main Menu loads and automatically applied when the player object initializes in the new scene.
* **Player State Persistence:** Refactored `GameSessionSO` to prevent premature snapshot clearing. Fixed the "Level 1 reset" bug by ensuring `PlayerProgression` and `PlayerStats` only initialize from snapshots if valid data exists.
* **Robust Polymorphic Serialization:** Upgraded `SaveManager.cs` to use `TypeNameHandling.All` and `MetadataPropertyHandling.ReadAhead`. This fixes the `JsonSerializationException` when handling abstract `ItemComponentState` classes (e.g., Consumables).
* **Metadata Peeking Fix:** Resolved compilation errors in `SaveManager.cs` by switching to `.ToObject<T>()` and implemented case-insensitive property lookups for more resilient metadata extraction.

## [Current/Recent] - Dynamic Main Menu Information & Active Slot Saving
* **Active Slot QuickSave:** Refactored `SaveManager.cs` to unify F5/F9 keys with the active save slot. Quick saving now writes directly to `save_1.json`, `save_2.json`, etc., based on the loaded session, eliminating the redundant `quicksave.json`.
* **Session Tracking:** Updated `GameSessionSO.cs` and `MainMenuController.cs` to track and persist the currently active `SaveSlotIndex` during gameplay.
* **Save Peeking Logic:** Implemented `PeekSaveMetadata` in `SaveManager.cs` using `JObject` to robustly bypass polymorphic type handling conflicts.

## [Current/Recent] - Connected Main Menu to Gameplay via Save System
* **Fresh Start Logic:** Implemented a fallback in `MainMenuController.cs` that recognizes empty slots and initializes a default game session in the "MainArea" scene.
* **File Path Fix:** Updated `SaveManager.cs` to use numerical indices for save files (e.g., `save_1.json`) instead of enum strings, ensuring consistency with the UI selection.
* **Load Integration:** Updated `MainMenuController.cs` to communicate with `SaveManager`, enabling real save data restoration from selected slots.
* **Scene Transition:** Integrated `SceneTransitionService` to automatically transport the player to the `CurrentSceneName` stored in the save file (with "MainArea" as a safe fallback).
* **Data Flow:** Verified that the `GameSessionSO` successfully imports the `GameSaveData` DTO before the scene load, ensuring the player's stats and inventory are ready upon arrival.

## [Current/Recent] - Refactor Main Menu to Mature MVP Architecture
* **Structural Decoupling:** Shifted responsibility for Save Slot instantiation and lifecycle management from `MainMenuController` to `MainMenuView`, ensuring the Presenter only handles high-level intent and data.
* **Data Transfer Objects:** Introduced `SaveSlotData` DTO to encapsulate save metadata, eliminating "Data Clump" anti-patterns in View signatures.
* **Memory Management:** Implemented explicit disposal logic for all dynamically generated sub-views within `MainMenuView.ClearSaveSlots()`, preventing event leaks and memory overhead.
* **Event Streamlining:** Consolidated individual sub-view selection events into a single `OnSaveSlotSelected(int index)` stream on the main View.

## [Current/Recent] - Fix Main Menu Save Slot Visibility
* Modified `SaveSlotView.cs` to set `m_HideOnAwake = false` in the constructor, preventing dynamically instantiated sub-views from defaulting to a hidden state during initialization.
* Updated `MainMenuController.cs` to explicitly call `slotView.Show()` after initialization in `GenerateMockSaveSlots`, ensuring the slots are visible when added to the `ScrollView`.
* Confirmed `MainMenuView.cs` correctly utilizes `contentContainer` for `Add` and `Clear` operations on the `ScrollView` to preserve its internal viewport and scrollbars.

## [Current/Recent] - Performance Optimization: PlayerBowController
* Cached `Camera.main` in `PlayerBowController.cs` to reduce overhead from frequent property access in `Update` and aim logic.
* Optimized `transform.position` usage in `PlayerBowController.GetAimDirection` by caching it in a local variable, minimizing native-to-managed bridge calls.

## [Current/Recent] - Documentation Update for Architecture Shifts
This update refactors the core documentation to accurately reflect the current "production-ready" architecture of the inventory and drag-and-drop systems, moving away from prototyping implementations.

### 1. Documented Shift from "Nuke" Redraw to Targeted Updates
* Updated `Inventory_Event_System_Documentation.md` and `Drag_and_Drop_System_Documentation.md` to detail the removal of the parameterless `OnInventoryUpdated` event.
* Added documentation for the new `OnSpecificSlotsUpdated(sourceSlot, targetSlot)` event.
* Documented the `PlayerInventoryView`'s use of a Dictionary mapping for targeted UI rebuilds, eliminating GC spikes and enabling large-scale slot optimization.

### 2. Documented Shift from Singleton Coupling to Event-Driven Modularity
* Updated `UI_Interactions_Documentation.md` to remove outdated references to the rigid `UIDragManager.Instance` singleton pattern.
* Documented the new decoupled, event-driven drag-and-drop system, highlighting how generic visual slots are now isolated and highly reusable across different contexts.

### 3. Documented Shift to Quantity-Based Transactions
* Updated `Inventory_Management_Documentation.md` and `UI_Interactions_Documentation.md` to explain the integration of an `amountToMove` integer.
* Detailed the central validation logic within the transfer manager, emphasizing its authoritative role as a "bank" using `Mathf.Min` to calculate safe transfers, permit stack splitting, and explicitly block partial-stack swaps to preserve game economy.

## [Current/Recent] - Enforce Item Type Validation for Drag-and-Drop Equipment Transfers

### 1. Data-Driven Slot Filters
* Extended `InventoryContainerSO` with an optional `PredefinedFilters` array of `SlotFilterType`.
* Updated `InventorySlot` constructor to accept and set a default `SlotFilterType`.
* Updated `InventoryManager` to initialize live slots using the predefined filters from its blueprint.

### 2. Configured Equipment Filters
* Updated `Container_Player-Equipments.asset` to map its 5 slots to specific filters: Head, Chest, Legs, Arms, Weapon. This ensures invalid items are blocked by `InventoryTransferManagerSO` during drag-and-drop.

### 3. Error Handling
* Added out-of-bounds error logging in `PlayerEquipmentController.ProcessSlot()` to catch configuration mismatches.

---

## [Previous] - Refactored Drag-and-Drop to Event-Driven Architecture

### 1. Removed Singleton Dependency
* Removed the Singleton pattern from `UIDragManager`, completely decoupling it from `InventorySlotView`.

### 2. Implemented Local Events
* Added `OnLocalDragStarted`, `OnLocalDragUpdated`, and `OnLocalDragStopped` events to `InventorySlotView`.

### 3. Updated Global Event Bus
* Added `OnGlobalDragStarted`, `OnGlobalDragUpdated`, and `OnGlobalDragStopped` to `UIInventoryEventsSO` to act as a global channel for visual drag states.

### 4. Added View Translation
* Updated `PlayerInventoryView`, `PlayerEquipmentView`, `ForgeSubView`, `SalvageSubView`, and `ShopSubView` to act as translators, listening to local slot drag events and forwarding them to the global `UIInventoryEventsSO`.

---

## [Previous] - Inventory Stack Splitting Support
- Added support for stack splitting using Shift-Click in the inventory drag-and-drop system. Players can now grab half a stack and drop it onto empty slots or stack it with other similar items.
- Refactored `InventoryTransferManagerSO` logic to dictate transfer quantity based on the UI event instead of blindly consuming the entire source slot count.
- Updated UI event pipeline (`UIInventoryEventsSO`, `InventorySlotView` and its subscribers) to pass `amountToMove` values.

## [Previous] - Decoupled Inventory Transfer Manager

### 1. Added SlotFilterType to InventorySlot
* Added a new `SlotFilterType` enum safely mapped to `EquipmentSlot` integer values.
* Added `AllowedFilter` field and `CanAccept(ItemInstance)` method to `InventorySlot` to enable data-level validation of item transfers, defaulting to `SlotFilterType.Any`.

### 2. Refactored InventoryTransferManagerSO
* Removed the hardcoded `IsValidEquipmentMove` method and screen type checks from `InventoryTransferManagerSO`.
* Updated `HandleMoveItemRequest` to cleanly execute the new `CanAccept()` checks on both the target slot and, in the case of a swap, the source slot.

---

## [Previous] - Skill Screen Architecture Implementation
This update introduces the foundational architecture for the Skill Screen, aligning with the project's MVC and Event Bus standards.

### 1. UI Layout & Styling (UXML/USS)
* **The Massive Canvas:** Utilized Flexbox inside a `ScrollView` (with hidden scrollbars) to create a large 1920x1080 panning area for the skill tree.
* **The Info Panel:** Built a static side panel to display detailed information (name, description, cost, status) and hold the primary action button.
* **Visual States:** Defined specific USS classes (`.skill-node--unlocked`, `--available`, `--locked`) to handle the coloring and opacity of nodes, plus pseudo-classes (`:disabled`, `:hover`) for the unlock button.

### 2. Static Data (ScriptableObjects)
* **The Blueprint:** Created `SkillData` ScriptableObjects to hold the static identity of each skill (ID, name, cost, text, and an array of prerequisite `SkillData`).
* **The Separation:** Ensures dynamic player save files are not bloated with static text and icon references.

### 3. Dynamic Data Model (Pure C#)
* **The Tracker:** Built `PlayerSkillTracker`, a pure, serializable C# class living inside `GameSessionSO` (the ultimate source of truth).
* **The Logic:** Securely handles deducting SP, adding unlocked IDs to a HashSet, and evaluating if prerequisites are met.

### 4. Event Bus Architecture (ScriptableObjects)
* **The Decoupling:** Created `UISkillEventsSO` to prevent the UI from directly modifying the save data.
* **The Manager:** Established a `SkillManager` MonoBehaviour to listen for the UI's `OnRequestUnlock` event, validate the math against the `GameSessionSO`, and broadcast back the success state.

### 5. The View (C# UI Logic)
* **The Mapping:** In `PlayerSkillView.cs`, used a Dictionary to map UXML `<ui:Button>` elements directly to their respective `SkillData` IDs.
* **Data Injection:** Injects SO data into the Info Panel labels when a node is clicked.
* **State Updates:** Wrote `RefreshAllNodes()`, sweeping the tree every time it opens (or a skill is bought) to dynamically add or remove the USS locked/available/unlocked classes based on the Tracker's logic.

### 6. The Controller (MonoBehaviour)
* **The Initialization:** Refactored `SkillScreenController.cs` to instantiate the UXML, inject the `SkillData[]` database and the `GameSessionSO` into the View, and register it directly into the `UIManager`'s FullScreen zone.

## [Current/Recent] - Localized UI Translation Layer
This update refactors `InventorySlotView` to decouple it from the global event bus, enforcing a stricter parent-child UI architecture and introducing an enum-based context state for inventory interactions.

### 1. Created `InventoryInteractionContext`
* Added the `InventoryInteractionContext` enum (Normal, Shop, Salvage) in its own file to track the current interaction mode without creating circular dependencies.
* Added an `OnInteractionContextChanged` action to `UIInventoryEventsSO` to allow dynamic UI views to broadcast context shifts.

### 2. Localized `InventorySlotView`
* Removed `UIInventoryEventsSO` dependency from `InventorySlotView` entirely.
* Replaced global triggers with local C# `Action` events (`OnLocalClicked`, `OnLocalRightClicked`, `OnLocalMoveItemRequested`, `OnLocalSelectForProcessingRequested`).
* This makes the slot view a pure, reusable component that blindly emits hardware interactions.

### 3. Updated Parent Views (The Translators)
* Updated `PlayerInventoryView`, `ShopSubView`, `SalvageSubView`, `ForgeSubView`, and `PlayerEquipmentView` to subscribe to the local slot events and act as pass-throughs to the global bus.
* `PlayerInventoryView` now listens to `OnInteractionContextChanged`. When a player right-clicks a slot, the view translates the action based on the active context (e.g., normal -> Equip, shop -> Sell, salvage -> Salvage).
* `PlayerEquipmentView` intentionally ignores context changes and strictly maps right-clicks to unequip actions, enforcing a safe two-step process for equipped items.
* `ShopSubView` and `SalvageSubView` now broadcast their context entry during `Show()` and reset to `Normal` during `Hide()`.

## [Previous] - Refactored UI Currency Access
* Replaced `PlayerProgressionAnchorSO` with `PlayerHUDBridge` in `ShopSubView` and related controllers (`SmithScreenController`, `MageScreenController`).
* UI views now strictly observe `PlayerHUDBridge.OnGoldChanged` instead of global event channels for currency updates.
* Removed redundant `OnCurrencyChanged` event from `UIInventoryEventsSO` to prevent race conditions.
* Updated `ShopManagerSO`, `SalvageManagerSO`, and `CraftingManagerSO` to not invoke `OnCurrencyChanged`.
* Removed unused `PlayerStatsAnchorSO` from `HudScreenController`.

## [Previous] - Cleanup redundant skill view script
This update removes redundant scripts for the skill tree UI and consolidates its logic into the actively used views and data structures.

### 1. Removed `SkillsView.cs`
* Deleted the older, redundant `SkillsView.cs` as its UI component functionality has been entirely replaced by `PlayerSkillView.cs`.

### 2. Consolidated Dependencies
* Moved the `SkillsPayload` data struct into `PlayerSkillView.cs` to prevent compilation errors after deletion.
* Validated `SkillsScreenController.cs` and `PlayerSkillView.cs` integration with the struct.

### 3. Documentation Updated
* Added Context-Dense Metadata Summaries in `Script_Descriptions/` for `PlayerSkillView.cs`, `SkillsScreenController.cs`, `SkillMenuController.cs`, and `SkillDataSO.cs`.

## [Previous] - Assign Skill Screen to Input Key
- Created `SkillMenuController.cs` to instantiate `InputSystem_Actions`, subscribe to the `ToggleSkills` performed event, and dispatch `UIEvents.OnRequestOpen` for the `SkillScreen`.

## [Previous] - Implemented Skills Screen UI Framework
- Created UXML and USS presentation assets for the new full-screen Skills interface.
- Implemented `SkillsView.cs` conforming to `GameView` principles (data-agnostic, delegates closing via events).
- Added `SkillsScreenController.cs` to instantiate the UI, bind the view, and register it to the `FullScreen` screen zone dynamically.

## [Previous] - Script Documentation Summaries
This update adds Context-Dense Metadata Summaries for several scriptable object scripts to improve project documentation and architecture visibility for AI agents.

### 1. Added Script Descriptions
* Generated detailed metadata summaries for `CraftingManagerSO`, `CraftingRecipeSO`, `CraftingRegistrySO`, `GameSessionSO`, `InventoryContainerSO`, and `InventoryItemSO`.
* Each summary outlines the Identifier, Architectural Role, Core Logic, Dependency Graph, Data Schema, and Side Effects & Lifecycle using a structured key-value format.
* Added all new files to `Toris/Assets/Documentation/Script_Descriptions/`.
## [Previous] - Script Metadata Documentation
This update adds Context-Dense Metadata Summaries for various core scripts to facilitate quick architectural understanding.

### 1. Added Script Descriptions
* Generated structured, key-value metadata `.md` files in `Toris/Assets/Documentation/Script_Descriptions/` for the following scripts:
  * `InventoryActionController`
  * `InventoryActionControllerDebugger`
  * `PlayerHUDBridge`
  * `ItemPickEventSO`
  * `InventorySlotTests`
## [Previous] - Script Documentation Generation
This update adds Context-Dense Metadata Summaries for four core scripts within the Inventory Item Architecture, adhering strictly to the structured key-value format required by the project directives.

### 1. Generated Documentation Summaries
* Created `ProgressionModule.md` documenting `ProgressionComponent`.
* Created `UpgradeableModule.md` documenting `UpgradeableComponent` and `UpgradeableState`.
* Created `ItemComponent.md` documenting the abstract base `ItemComponent` class.
* Created `ItemComponentState.md` documenting the abstract base `ItemComponentState` class.

### 2. Standardization
* Ensured all generated files adhere to the strict key-value formatting rules: Identifier, Architectural Role, Core Logic, Dependency Graph, Data Schema, and Side Effects & Lifecycle.
* Omitted conversational language and used technical shorthand with bullet points.
## [Previous] - Script Metadata Documentation
This update adds Context-Dense Metadata Summaries for several script files to act as primary references for AI agents.

### 1. Created Script Summaries
* Created `IContainerInteractable.md` detailing the Interface architecture for interactive containers.
* Created `ConsumableModule.md` detailing the Abstract Blueprint and Runtime State architecture for consumable items.
* Created `DefensiveModule.md` detailing the Data Container architecture for defensive item stats.
## [Previous] - Added Script Metadata Summaries
This update adds structured context-dense metadata summaries for UI Toolkit screen controllers to aid AI agents and developers in understanding the codebase architecture.

### 1. Created Metadata Summaries
* Generated context-dense key-value documentation in `Toris/Assets/Documentation/Script_Descriptions/` for:
  * `HudScreenController.md`
  * `InventoryScreenController.md`
  * `MageScreenController.md`
  * `MainMenuScreenController.md`
  * `SmithScreenController.md`
## [Previous] - Script Metadata Summaries Added
This update adds Context-Dense Metadata Summaries for several UI-related scripts to serve as primary references for AI agents, following a highly structured format.

### 1. Created Script Descriptions
* Created `ForgeSubView.md`, `GameView.md`, `HUDView.md`, and `MageView.md` inside `Toris/Assets/Documentation/Script_Descriptions/`.
* Each summary outlines the script's Architectural Role, Core Logic (Abstract/Virtual Methods, Public API), Dependency Graph, Data Schema, and Side Effects & Lifecycle using key-value bulleted formats.

---

## [Previous] - Documentation Updates
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

## [Current/Recent] - Consolidated Item and Inventory Architecture Documentation
### 1. Centralized Documentation
* Created a new folder `Toris/Assets/Documentation/Item_Architecture/` to hold centralized information.
* Created `Complete_Item_And_Inventory_Architecture.md`, consolidating information about the Item Blueprint/State pattern, Inventory Data Management, Event Systems, and Drag-and-Drop UI interactions.

### 2. Cleaned Up Old Fragments
* Deleted `Item_Architecture_Documentation.md`, `Inventory_Management_Documentation.md`, `Inventory_Event_System_Documentation.md`, and `UI_Interactions_Documentation.md` as their content is now fully integrated into the centralized document.

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

## [Current/Recent] - UI Inventory Events Compilation Fix
This update resolves a compilation error in `UIInventoryEventsSO.cs` caused by exceeding the maximum number of type arguments supported by `UnityAction`.

### 1. Fixed `OnRequestMoveItem` Delegate
* Changed the `OnRequestMoveItem` event from `UnityAction` to `System.Action` to support 5 type arguments (`InventoryManager`, `InventorySlot`, `InventoryManager`, `InventorySlot`, `int`).

---

## [Unreleased]
### Added
- Created `IUsable` and `IEquipable` interfaces in `OutlandHaven.Inventory` to establish capability contracts for items.
- Added caching properties `UsableBehavior` and `EquipableBehavior` to `InventoryItemSO` to provide O(1) access to item capabilities.

### Changed
- Refactored `ConsumableComponent` and `EquipableComponent` to implement their respective capability interfaces.
- Migrated consumption routing logic from `PlayerConsumableController.TryUseConsumable` to `ConsumableComponent.TryUse`.
- Refactored `InventoryActionController` to utilize cached interface properties (`UsableBehavior`, `EquipableBehavior`) instead of O(N) `GetComponent<T>()` lookups.
- **UI Architecture:** Fixed broken drag-and-drop and click interactions on dynamically instantiated UI Toolkit inventory slots by updating the `TemplateContainer` wrapper's picking mode to `Ignore` and correctly registering pointer events directly onto the inner `.item-slot` element in `InventorySlotView.cs`.
# General Project Changelog

**Rules:**
* Archive previous changes and add new ones at the top to continue the log.
* Shortly describe what was done.
* Enumerate or mark different changes; if changes are too big, divide them into smaller ones.

---

## [Previous] - Clean up Leftover Python Scripts
* Deleted leftover Python scripts (`*.py`) from the root directory that were accumulated during previous pull requests.

## [Previous] - Script Metadata Summaries
This update adds structured context-dense metadata summaries for item entity modules to aid in scaling and dependency tracking.

### 1. EquipableModule Summary
* Added `Toris/Assets/Documentation/Script_Descriptions/EquipableModule.md`.
* Documented `EquipableComponent` as an abstract blueprint, listing its schema (`TargetSlot`, `StrengthBonus`, `DefenceBonus`) and its downstream UI/Effect system dependencies.

### 2. EvolvingItemModule Summary
* Added `Toris/Assets/Documentation/Script_Descriptions/EvolvingItemModule.md`.
* Documented both the static blueprint (`EvolvingComponent`) and its dynamic runtime tracker (`EvolvingState`), including abstract method overrides for stacking and cloning.

### 3. OffensiveModule Summary
* Added `Toris/Assets/Documentation/Script_Descriptions/OffensiveModule.md`.
* Documented `OffensiveComponent` emphasizing its static nature (no runtime state needed) and data schema (`BaseDamage`, `AttackSpeed`).
## [Previous] - Script Metadata Documentation
This update adds Context-Dense Metadata Summaries for several script files as part of expanding the AI assistant documentation context.

### 1. Created Script Descriptions
* Added `SalvageManagerSO.md`, `SalvageRecipeSO.md`, `ShopManagerSO.md`, `UpgradeSalvageManagerSO.md`, and `ItemTestDebugger.md` to `Toris/Assets/Documentation/Script_Descriptions/`.
* Ensured summaries are highly token-efficient and use a structured key-value format without conversational language.
## [Previous] - Documentation Updates
This update addresses the generation of Context-Dense Metadata Summaries for several UI and systemic classes, expanding the `Script_Descriptions` folder to aid in modular code comprehension.

### 1. Generated Summaries
* Created `UIInventoryEventsSO.md` detailing the decoupled event channel for UI inventory interactions.
* Created `SystemBootstrapper.md` detailing the global entry point for persistent manager initialization.
* Created `UIDragManager.md` detailing the UI pointer tracking and global drag visual layer.
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

## [Current/Recent] - UI Inventory Events Compilation Fix
This update resolves a compilation error in `UIInventoryEventsSO.cs` caused by exceeding the maximum number of type arguments supported by `UnityAction`.

### 1. Fixed `OnRequestMoveItem` Delegate
* Changed the `OnRequestMoveItem` event from `UnityAction` to `System.Action` to support 5 type arguments (`InventoryManager`, `InventorySlot`, `InventoryManager`, `InventorySlot`, `int`).

---

## [Unreleased]
### Added
- Created `IUsable` and `IEquipable` interfaces in `OutlandHaven.Inventory` to establish capability contracts for items.
- Added caching properties `UsableBehavior` and `EquipableBehavior` to `InventoryItemSO` to provide O(1) access to item capabilities.

### Changed
- Refactored `ConsumableComponent` and `EquipableComponent` to implement their respective capability interfaces.
- Migrated consumption routing logic from `PlayerConsumableController.TryUseConsumable` to `ConsumableComponent.TryUse`.
- Refactored `InventoryActionController` to utilize cached interface properties (`UsableBehavior`, `EquipableBehavior`) instead of O(N) `GetComponent<T>()` lookups.
- **UI Architecture:** Fixed broken drag-and-drop and click interactions on dynamically instantiated UI Toolkit inventory slots by updating the `TemplateContainer` wrapper's picking mode to `Ignore` and correctly registering pointer events directly onto the inner `.item-slot` element in `InventorySlotView.cs`.
