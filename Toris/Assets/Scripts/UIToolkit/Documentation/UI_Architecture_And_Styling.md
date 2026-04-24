# UI Architecture and Styling Documentation

This document outlines the architecture and conventions for the UI system used in Outland Haven, leveraging Unity's UI Toolkit.

## 1. Core Architectural Pillars

To ensure scalability and prevent tight coupling, all UI development must adhere to the following pillars:

*   **Strict MVP Pattern:** UI Views (`UIView`, `GameView`) are completely "dumb." They never hold game logic, modify stats, or process transactions. Logic resides entirely in Presenters/Controllers (e.g., `HudScreenController`, `InventoryScreenController`) and dedicated System Managers.
*   **Event-Driven Communication:** Systems communicate via `ScriptableObject` events (e.g., `UIEventsSO`, `UIInventoryEventsSO`). A UI button click does not call a manager directly; it fires an event that the manager listens for.
*   **The Architecture of Restraint:** The UI enforces gameplay constraints rather than providing all options constantly.

## 2. Layout Structures & Controllers

All UI windows operate under a unified lifecycle managed by the `UIManager`.

### 2.1 The UIManager (`UIManager.cs`)
The `UIManager` is the central authority attached to the root `UIDocument`.
*   **ScreenZones:** Defines layout areas via `ScreenZone` (HUD, Left, Right, Modal).
*   **View Registration:** It accepts `GameView` instances via `RegisterView(GameView view, ScreenZone zone)` and appends their root `VisualElement` to the specified zone.
*   **Mutual Exclusivity:** `UIManager` enforces mutual exclusivity for views sharing the same `ScreenZone` (excluding HUD). Opening a new screen automatically closes the previous one in that zone.

### 2.2 Screen Types & Zones (`ScreenTypes.cs`)
*   `ScreenType`: Identifies the specific functional screen (e.g., `HUD`, `Inventory`, `Skills`, `Smith`). This is used for event payloads to open/close specific screens via `UIEventsSO`.
*   `ScreenZone`: Defines *where* a screen should be placed within the main layout.

### 2.3 UI Timing Conventions
Controllers act as the bridge between Unity's scene lifecycle and the pure C# UI Views.
*   **`Awake()`**: Used for early initialization, such as querying for specific layout zones within the `UIDocument`.
*   **`OnEnable()`**: Used for validation (checking for missing serialized references), finding UI elements, and subscribing to global UI events (e.g., `OnRequestOpen`).
*   **`Start()`**: Used for assembling the UI, instantiating templates, creating View instances, and registering those Views with the central `UIManager`.

## 3. Strict UI Toolkit Styling Conventions

The project enforces a highly decoupled, component-based UI architecture. All modifications to `.uxml`, `.uss`, and UI-related `.cs` files must adhere to the following rules:

### 3.1 The Inline Style Purge (Strict Separation of Concerns)
*   **The Rule:** UXML is strictly for DOM hierarchy and structural layout. USS is strictly for styling.
*   **Execution:** Absolutely zero `style="..."` attributes are allowed in any `.uxml` file. Use semantic classes (e.g., `class="panel-header"`) and define layouts in the `.uss` file.

### 3.2 Global Variable Enforcement (Theme Consistency)
*   **The Rule:** Hardcoded `rgb()`, `rgba()`, `#hex` values, and raw font sizes are strictly forbidden in standard stylesheets.
*   **Execution:** Every color, border, and typography size must reference a `var(--...)` property from `theme-variables.uss`. Stick exclusively to the established centralized palette (gritty, grounded medieval aesthetic).

### 3.3 Selector Specificity & Data Binding Isolation
*   **The Rule:** CSS styling must never use ID selectors (`#Name`).
*   **Execution:** The `name="..."` attribute in UXML acts exclusively as a lookup key for C# scripts (e.g., `root.Q<VisualElement>("Equipment__Header")`). All USS styling must be done via reusable class selectors (e.g., `.player-equipment-header`).

### 3.4 Flexbox Intent & Squish Prevention
*   **The Rule:** Elements must predictably flow and scale.
*   **Execution:**
    *   **Rigid Elements:** Any element with specific dimensional requirements must include `flex-shrink: 0;` to prevent squishing.
    *   **Structural Containers:** Wrappers meant to fill remaining space must explicitly include `flex-grow: 1;`.
