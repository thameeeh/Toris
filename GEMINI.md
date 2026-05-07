# Outland Haven - Gemini Project Mandates

This file contains foundational mandates for Gemini CLI. These rules take absolute precedence over general defaults. Do not bypass these rules for "quick fixes."

## Project Scope & Focus
- **Primary Focus:** UI (specifically UI Toolkit), Inventory Systems, Item Systems, and Save/Load Data Architecture.
- **Restriction:** Do not work on or modify systems outside this scope (e.g., combat, world generation, AI) unless explicitly instructed otherwise.

## Core Architectural Mandates (MVP & Events)
- **Strict MVP Pattern:** Enforce a strict separation of Model, View, and Presenter.
- **Models:** Serve as the single source of truth, validating data and broadcasting state changes without any UI knowledge.
- **Views (UIView/GameView):** Remain purely visual, "dumb" C# classes that construct the visual hierarchy and capture inputs. 
- **Presenters (Controllers):** Act as `MonoBehaviour` orchestrators that bridge Unity's lifecycle, backend Models, and Views.
- **Unidirectional Data Flow:** Views capture hardware inputs and broadcast semantic intents (e.g., `OnRequestSell`); they never process transactions directly.
- **Event-Driven Architecture:** All system communication must flow through `ScriptableObject` events (e.g., `UIEventsSO`).
- **No God Objects:** Avoid `GameObject.Find()` and do not perform complex calculations inside UI scripts.

## Controller & View Lifecycle
- **Awake() (Controllers):** Use exclusively for early initialization and querying layout zones within the root `UIDocument`.
- **OnEnable() (Controllers):** Use for establishing UI element references and subscribing to global UI events.
- **Start() (Controllers):** Use for assembling the UI, instantiating templates, and registering Views.
- **Window Management:** Controllers must register top-level `GameView` instances with the `UIManager` using `RegisterView(GameView view, ScreenZone zone)`.
- **Memory Management:** `UIView` instances must implement `IDisposable`.
- **Disposal:** Always override `Dispose()` to explicitly unregister callbacks and detach global event listeners when UI is destroyed.

## UI Toolkit Styling & DOM Rules
- **No Inline Styles:** Absolutely zero `style="..."` attributes are allowed in `.uxml` files; use semantic classes instead.
- **Theme Consistency:** Hardcoded hex values and raw font sizes are forbidden in standard stylesheets.
- **Variables:** Every color, border, and typography size must reference a `var(--...)` property from `theme-variables.uss`.
- **No ID Selectors:** CSS styling must never use ID selectors (`#Name`); rely entirely on reusable class selectors.
- **Query Keys:** The `name="..."` attribute in UXML acts exclusively as a lookup key for C# scripts.
- **State Toggling:** Use USS classes for state changes (e.g., `element.AddToClassList("hidden")`) rather than toggling display properties via C#.
- **Flexbox Sizing:** Include `flex-shrink: 0;` in USS for rigid elements and `flex-grow: 1;` for structural containers to prevent squishing.
- **Runtime Wrappers:** Explicitly apply `style.flexGrow = 1` in C# to dynamically instantiated SubViews or Panels before adding them to a parent.
- **Third-Party Isolation:** Treat third-party UI files as read-only; use custom `.uss` override files attached to the root UI Document for styling adjustments.

## Inventory & Items
- **Persistence:** Items use `ItemInstance` with `[SerializeReference]` for dynamic state.
- **Transactions:** Pass specific `ItemInstance` objects (not blueprints) to preserve unique states (durability, level, etc.).
- **Safety:** Cache `InventoryItemSO` references before modifying collections to avoid `NullReferenceException` on lookups.

## Performance & Engineering Standards
- **Environment:** Use `dotnet 10.0.103`.
- **Serialization:** Use `Newtonsoft.Json` for handling `[SerializeReference]` serialization in Save/Load scripts.
- **Caching:** Cache `Camera.main` and `transform.position` in local variables during loops/Updates.
- **Math:** Use `sqrMagnitude` for distance checks.
- **Cleanup:** Remove empty Unity lifecycle methods (`Update`, `Start`, etc.).
- **Logging:** Prepend `[UI/Inventory]` to `Debug.Log` statements for easier filtering.
- **Validation:** Prioritize static analysis and logical dry-runs.
- **Changelog:** Always update `Toris/Assets/Scripts/UIToolkit/Documentation/Changelog/CHANGELOG.md` after completing a task.