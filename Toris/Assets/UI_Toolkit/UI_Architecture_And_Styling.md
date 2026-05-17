OUTLAND HAVEN UI TOOLKIT: STRICT AI DEVELOPMENT DIRECTIVE
---------------------------------------------------------

**Context & Purpose:** This document enforces the architecture, logic separation, and UI Toolkit conventions for the "Outland Haven" project. AI agents and code generators must treat these rules as absolute constraints. Do not attempt to bypass these rules for "quick fixes."

1.Core Architectural Constraints (Logic & Flow)
---------------------------------------------

All UI code generated must strictly adhere to a decoupled, event-driven architecture.

- **Strict MVP Pattern Enforcement:** UI Views (`UIView`, `GameView`) must remain completely "dumb." They must never execute game logic, modify underlying stats, or process transactions. All logic resides strictly within Presenters/Controllers (e.g., `HudScreenController`) and designated System Managers.
- **Event-Driven Communication:** Do not write UI code that calls Managers directly. All system communication must flow through `ScriptableObject` events (e.g., `UIEventsSO`). A UI interaction (like a button click) must solely fire an event payload; it is the Manager's job to listen and react.
- **Controller Lifecycle Strictness:**
  - Use `Awake()` exclusively for early initialization (e.g., querying for specific layout zones within the root `UIDocument`).
  - Use `OnEnable()` for validation, establishing UI element references, and subscribing to global UI events (e.g., `OnRequestOpen`).
  - Use `Start()` for assembling the UI, instantiating templates, creating View instances, and registering those Views.

2.Window Management (UIManager)
-----------------------------

Do not write custom logic to toggle UI window visibility manually. All UI screens operate under the unified lifecycle of the `UIManager.cs`.

- **Screen Registration:** Controllers must register their `GameView` instances with the `UIManager` using `RegisterView(GameView view, ScreenZone zone)`.
- **Screen Zones:** Ensure views are appended to the correct layout area (`ScreenZone`: HUD, Left, Right, Modal).
- **Mutual Exclusivity:** Rely on the `UIManager` to enforce mutual exclusivity. Opening a new screen in a specific `ScreenZone` will automatically handle closing the previous one.

3.UI Toolkit Styling & DOM Rules
------------------------------

All modifications to `.uxml`, `.uss`, and UI-related `.cs` files must adhere to the following execution rules.

*3.1 The Inline Style Purge (Strict Separation of Concerns)*

- **The Rule:** UXML is strictly for DOM hierarchy and structural layout. USS is strictly for styling.
- **Execution:** Absolutely zero `style="..."` attributes are allowed in any `.uxml` file. If an element requires specific styling, you must assign it a semantic class (e.g., `class="panel-header"`) and define the layout in the corresponding `.uss` file.

*3.2 Global Variable Enforcement (Theme Consistency)*

- **The Rule:** Hardcoded `rgb()`, `rgba()`, `#hex` values, and raw font sizes are strictly forbidden in standard stylesheets.
- **Execution:** Every color, border, and typography size must reference a `var(--...)` property from `theme-variables.uss`. Stick exclusively to the established centralized palette (gritty, grounded medieval aesthetic utilizing low-saturation wood and stone tones).

*3.3 Selector Specificity & Data Binding Isolation*

- **The Rule:** CSS styling must never use ID selectors (`#Name`).
- **Execution:** The `name="..."` attribute in UXML acts exclusively as a highly performant lookup key for C# scripts (e.g., `root.Q<VisualElement>("Equipment__Header")`). All USS styling must be done via reusable class selectors (e.g., `.player-equipment-header`).

*3.4 Flexbox Intent & Squish Prevention*

- **The Rule:** Elements must predictably flow and scale. Do not rely on UI Builder's default assumptions.
- **Execution:**
  - **Rigid Elements:** Any element with specific dimensional requirements (like an 82x82px item slot) must include `flex-shrink: 0;` in its USS class to prevent the layout engine from squishing it.
  - **Structural Containers:** Any wrapper meant to fill remaining space must explicitly include `flex-grow: 1;`.

*3.5 The Runtime Integration Check (C# Wrappers)*

- **The Rule:** Dynamically spawned UXML templates are wrapped in a hidden `TemplateContainer` that defaults to a collapsed size at runtime.
- **Execution:** When writing C# screen controllers that dynamically instantiate full SubViews or Panels via `.Instantiate()`, you must explicitly apply `style.flexGrow = 1` to the spawned instance before adding it to the parent container. *(Note: Do not apply this to small, fixed-size atomic components like individual item slots).*

*3.6 Third-Party Plugin Isolation*

- **The Rule:** Never modify the source `.uxml` or `.uss` files of third-party assets (e.g., external Dialogue Systems).
- **Execution:** Treat third-party UI files as read-only. To alter their appearance, generate a custom `.uss` file in the project's own UI directory, target the third-party classes, write override rules, and attach the custom stylesheet to the root UI Document.

*3.7 Centering & Symmetrical Anchoring*

- **The Rule:** Critical HUD elements (like Potion Bars or Crosshairs) should be centered independently of surrounding flex-flow siblings.
- **Execution:** Use `position: absolute; left: 50%; translate: -50% 0;`. This ensures the element is anchored to the dead-center of the screen regardless of the width or presence of left/right HUD containers.

*3.8 Adaptive HUD Zones*

- **The Rule:** Avoid hardcoded percentage heights for HUD layers (e.g., `height: 20%`) to prevent resolution clipping or excessive gaps.
- **Execution:** Set HUD layers to `height: auto;` and anchor them using `position: absolute; bottom: 0;`. This allows the pixel-height of the HUD elements to dictate the zone size, ensuring consistent spacing across 720p, 1080p, and 4K.