# Toris Game Project - AGENTS.md

This file contains crucial architectural directives, conventions, and guidelines for AI agents working on this project. Always adhere strictly to these rules.

## Core Architectural Directives
1. **Data-Driven Approach**: Use ScriptableObjects for flyweight items and game data.
2. **MVP Pattern for UI**: Views must be "dumb". All logic must reside in pure C# Presenters or Managers.
3. **Event-Driven Architecture**: Use Observer pattern/Event Bus via ScriptableObjects (e.g., `UIEventsSO`, `UIInventoryEventsSO`) to prevent hard coupling.
4. **Composition over Inheritance**: Use component-based logic and Command pattern for transactions.
5. **Avoid God Objects**: Do not perform mathematical calculations in UI scripts, and absolutely avoid hard references like `GameObject.Find()`.

## UI Architecture (UI Toolkit & MVP)
- **Data Decoupling**: UI View classes (e.g., `ShopSubView`) must NEVER directly modify game data (like item counts) to prevent exploitation. Dispatch action events and listen for state confirmation events to rebuild visuals based on authoritative data.
- **UI Controllers**: Do not use `UIDocument` components for UI Controllers. Instead:
  - Require a `VisualTreeAsset` (UXML).
  - Instantiate it in the `Start()` method (not `OnEnable()`, to avoid race conditions with `UIManager.Awake()`).
  - Pass the instance to the View, explicitly call `view.Initialize()` (never initialize within the View constructor to avoid race conditions).
  - Register the view with the `UIManager` using the appropriate `ScreenZone`.
- **View Lifecycle**:
  - `SetVisualElements()` must STRICTLY only query and cache static layout elements (`.Q<VisualElement>()`) without touching injected data.
  - `Setup()` is designated for dynamic assembly, such as instantiating sub-templates and populating data grids. Always explicitly call `view.Setup(payload)` before `view.Show()` when programmatically opening dependent views.
  - Bind/unbind dynamic event listeners in `Show()`/`Hide()` or `Setup()`/`Dispose()` rather than the constructor. Track event subscription state with a `private bool _eventsBound` to prevent double subscriptions and leaks.
- **Screen Zones**: `UIManager` enforces mutual exclusivity for views sharing the same `ScreenZone` (excluding the `HUD` zone). Opening a new view automatically closes any active view in that zone.
- **Styling**: When modifying layouts, adjust UXML/USS files for layout and sizing instead of using C# inline styles (e.g., `element.style.width = ...`), which override all other stylesheets and cause regressions.
- **Sub-Views & Tabs**: For tabbed UI screens, the main ScreenController must serialize and pass required sub-view `VisualTreeAsset` templates to the main View. The View lazily instantiates these templates upon tab selection and handles their `Hide()` and `Dispose()` lifecycles.
- **UXML Organization**: UI Toolkit sub-view UXML files go into `Toris/Assets/UI Toolkit/UXML Templates/`.
- **Input Slots**: When displaying selected inventory items in crafting/input slots, instantiate a dummy proxy `InventorySlot` with the exact required quantity rather than passing the direct player inventory slot reference.

## Inventory & Crafting System
- **Item Instances**: The inventory uses an `ItemInstance` class to store runtime state dynamically via a `[SerializeReference]` list of `ItemComponentState` objects. These are generated via a Factory Method (`CreateInitialState`) defined on strictly isolated `ItemComponent` blueprints attached to the `InventoryItemSO`. Hardcoded properties like Level or Durability must not exist in `ItemInstance`.
- **Transactions**:
  - For shop transactions, pass the specific `ItemInstance` object instead of generating a new one from the blueprint to preserve unique dynamic states.
  - When performing multiple item removals based on UI slot references, ALWAYS cache the `InventoryItemSO` base references first. Modifying the inventory can clear the underlying stack, causing a `NullReferenceException` on subsequent `slot.HeldItem` lookups.
- **Shops**: A single `ShopManagerSO` handles transactions. Dynamic NPC inventories are set by intercepting payloads via `UIEvents.OnRequestOpen`. If opened via UI hotkeys (null payload), the specific screen's UI Controller must manually inject its `_shopContainer` into `ShopManagerSO`.
- **Crafting & Salvaging**: Modular ScriptableObjects are used (`CraftingRecipeSO`, `SalvageRecipeSO`, `CraftingRegistrySO`). Dedicated managers handle transaction logic (`CraftingManagerSO`, `SalvageManagerSO`). UI action buttons must validate player's total inventory count. Item selection in UI visually copies the item; it is not removed until the final action is confirmed and executed by its manager.

## Unity Performance & Optimization
- **Component Caching**: Cache component properties like `transform.position` to local variables outside of loops to prevent repeatedly calling into the native C++ engine side.
- **TryGetComponent**: Prefer using `TryGetComponent<T>(out var component)` instead of `GetComponent<T>()` followed by a null check, especially in physics trigger events to avoid allocations and CPU overhead.
- **Distance Comparisons**: Prioritize using `sqrMagnitude` against a squared threshold instead of `Vector3.Distance` or `Vector3.magnitude` to eliminate expensive square root calculations.
- **Empty Lifecycles**: Remove empty Unity lifecycle methods (e.g., `Start`, `Update`, `FixedUpdate`) to minimize C++ to C# bridge overhead.
- **Pooled Objects**: For pooled Map Generation POIs (e.g., 'WolfDen'), cache child components like `Collider2D[]` in `Awake` using `GetComponentsInChildren(true)` to prevent performance degradation from hierarchy traversals during frequent `Initialize` or `Clear` cycles.
- **Performance Journal**: Maintain a performance journal in `.jules/bolt.md` for critical, codebase-specific performance learnings. Format: `## YYYY-MM-DD - [Title]`, `**Learning:** [Insight]`, and `**Action:** [How to apply next time]`. Do not commit this journal.

## Code Health & Security Practices
- **Magic Numbers**: Extract magic numbers to `const` or serialized fields. Unresolved magic numbers should be documented in `Toris/Assets/Scripts/MagicNumbers.md`.
  - For world generation `DeterministicHash.Hash`, use named constants for specific integer salts (e.g., `HASH_SALT_GROUND = 101`, `ROAD_HASH_SALT = 5001`).
- **Debugging**: Wrap `Debug.Log` statements in `#if UNITY_EDITOR` preprocessor directives. Remove dead, commented-out debug statements and proactively clean up resulting unused variables or empty control blocks.
- **Save System**: The Pixel Crushers Save System integration must NOT have hardcoded secrets. Save system encryption defaults must be set to `encrypt = false` with an empty password string.
- **Serialization**:
  - Use custom Editor scripts to instantiate abstract classes in `[SerializeReference]` lists via a Reflection-based `GenericMenu`.
  - Enforce sensible defaults using parameterless constructors and an `OnValidate()` method wrapped in `#if UNITY_EDITOR` for custom serializable classes to prevent game crashes. Avoid validating dynamically generated generic states via `OnValidate`.
  - Allow legacy test/dummy data in serialized assets to wipe rather than writing custom C# Editor migration scripts.
- **Input System**: When modifying Unity Input System mappings outside the Editor, always update both the `.inputactions` JSON asset and its corresponding auto-generated C# script (`InputSystem_Actions.cs`) synchronously.
- **Legacy Systems**: The 'GameInitiator' system and related UIs (MainMenuUI, PauseMenuUI) have been completely removed. Do not use or reference them.

## Testing & Tooling
- **Test Execution**: The environment lacks a headless Unity Editor executable. Local verification relies on static analysis, logical dry-runs, theoretical rationales in PRs, and CI pipeline execution. `dotnet build` and CLI test execution without Unity Editor initialization are restricted due to lack of .asmdef and .sln files.
- **Testability**: Implement public getters in ScriptableObjects (e.g., `PlayerDataSO`) to allow NUnit test fixtures to verify internal state updates. Unit testing is established in Editor folders (e.g., `Toris/Assets/Scripts/UIToolkit/Editor/Tests/`).
- **Dotnet**: Use `dotnet` version 10.0.103.

## Pull Request Guidelines
Follow strict PR title and description formatting based on the PR type:
- **Performance Improvement**:
  - Title: `⚡ [performance improvement description]`
  - Description sections: `💡 What`, `🎯 Why`, `📊 Measured Improvement` (or documented rationale).
- **Code Health Improvement**:
  - Title: `🧹 [code health improvement description]`
  - Description sections: `What`, `Why`, `Verification`, `Result`.
- **Testing Improvement**:
  - Title: `🧪 [testing improvement description]`
  - Description sections: `🎯 What`, `📊 Coverage`, `✨ Result`.
- **Security Fix**:
  - Title: `🔒 [security fix description]`
  - Description sections: `🎯 What`, `⚠️ Risk`, `🛡️ Solution`.

## Documentation
- All project markdown documentation is centralized in `Toris/Assets/Documentation/`.
- Relevant detailed docs include `Inventory_Event_System_Documentation.md`, `UI_System_Documentation.md`, `General_Scripting_Conventions.md`, etc.
- **MANDATORY**: On every new Pull Request or completed task, you MUST update the general project changelog located at `Toris/Assets/Documentation/Changelog/CHANGELOG.md`. Archive previous changes and add new ones at the top of the log. Also, update any other relevant documentation files to reflect your changes.
