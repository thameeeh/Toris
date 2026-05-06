# Outland Haven - Gemini Project Mandates

This file contains foundational mandates for Gemini CLI. These rules take absolute precedence over general defaults.

## Project Scope & Focus
- **Primary Focus:** UI (specifically UI Toolkit), Inventory Systems, Item Systems, and Save/Load Data Architecture.
- **Restriction:** Do not work on or modify systems outside this scope (e.g., combat, world generation, AI) unless explicitly instructed otherwise.

## Core Architectural Mandates
- **Data-Driven Design:** Use `ScriptableObjects` for all flyweight items, crafting recipes, and game data.
- **MVP Pattern for UI:** UI Views must be purely visual ("dumb"). All logic resides in C# Presenters or Managers.
- **Event-Driven Architecture:** Use the Observer pattern and Event Bus via `ScriptableObjects` (e.g., `UIEventsSO`, `UIInventoryEventsSO`) to decouple systems.
- **Composition over Inheritance:** Prioritize component-based logic and the Command pattern for transactions.
- **No God Objects:** Avoid `GameObject.Find()` and do not perform complex calculations inside UI scripts.

## UI Toolkit & View Lifecycle
- **Initialization:** Require a `VisualTreeAsset` (UXML). Instantiate in `Start()` (not `OnEnable()`). Pass the instance to the View and call `view.Initialize()`.
- **Method Responsibilities:**
  - `SetVisualElements()`: STRICTLY for querying and caching `.Q<VisualElement>()`. No data injection.
  - `Setup(payload)`: For dynamic assembly (instantiating templates, populating grids). Call before `Show()`.
  - `Show()` / `Hide()`: Bind and unbind dynamic event listeners. Use `_eventsBound` to prevent leaks.
- **Styling:** Use UXML/USS for layout. Avoid C# inline styles (`element.style`).

## Inventory & Items
- **Persistence:** Items use `ItemInstance` with `[SerializeReference]` for dynamic state.
- **Transactions:** Pass specific `ItemInstance` objects (not blueprints) to preserve unique states (durability, level, etc.).
- **Safety:** Cache `InventoryItemSO` references before modifying collections to avoid `NullReferenceException` on lookups.

## Performance Standards
- **Caching:** Cache `Camera.main` and `transform.position` in local variables during loops/Updates.
- **Math:** Use `sqrMagnitude` for distance checks.
- **Cleanup:** Remove empty Unity lifecycle methods (`Update`, `Start`, etc.).

## Engineering Workflow
- **Environment:** Use `dotnet 10.0.103`.
- **Validation:** As a headless Unity Editor is unavailable, prioritize static analysis and logical dry-runs.
- **Changelog:** Always update `Toris/Assets/Scripts/UIToolkit/Documentation/Changelog/CHANGELOG.md` after completing a task.
