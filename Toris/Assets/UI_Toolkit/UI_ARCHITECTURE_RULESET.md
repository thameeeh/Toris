OUTLANDHAVEN UI TOOLKIT: STRICT DEVELOPMENT RULESET

Directive for AI Agents & Developers: This project strictly enforces a decoupled, component-based UI architecture. Do not attempt to "quick fix" layouts. All modifications to .uxml, .uss, and UI-related .cs files must adhere to the following rules without exception.
1. The Inline Style Purge (Strict Separation of Concerns)

    The Rule: UXML is strictly for DOM hierarchy and structural layout. USS is strictly for styling.

    Execution: Absolutely zero style="..." attributes are allowed in any .uxml file. If an element requires specific styling, you must assign it a semantic class (class="panel-header") and define the layout in the corresponding .uss file.

2. Global Variable Enforcement (Theme Consistency)

    The Rule: Hardcoded rgb(), rgba(), #hex values, and raw font sizes are strictly forbidden in standard stylesheets.

    Execution: Every color, border, and typography size must reference a var(--...) property from theme-variables.uss. The visual direction is a gritty, grounded medieval aesthetic utilizing low-saturation wood and stone tones. Do not introduce high-saturation colors or generic gray-box colors; stick exclusively to the established centralized palette.

3. Selector Specificity & Data Binding Isolation

    The Rule: CSS styling must never use ID selectors (#Name).

    Execution: The name="..." attribute in UXML acts exclusively as a highly performant lookup key for C# scripts (e.g., root.Q<VisualElement>("Equipment__Header")). All USS styling must be done via reusable class selectors (e.g., .player-equipment-header). This ensures UI components remain visually reusable while preserving strict, decoupled C# data bindings.

4. Flexbox Intent & Squish Prevention

    The Rule: Elements must predictably flow and scale. Do not rely on UI Builder's default assumptions.

    Execution: * Rigid Elements: Any element with specific dimensional requirements (like an 82x82px item slot) must include flex-shrink: 0; in its class to prevent the layout engine from squishing it.

        Structural Containers: Any wrapper meant to fill remaining space must explicitly include flex-grow: 1;.

5. The Runtime Integration Check (C# Wrappers)

    The Rule: UI Builder previews differ from Unity runtime instantiation. Dynamically spawned UXML templates are wrapped in a hidden TemplateContainer that defaults to a collapsed size.

    Execution: When writing C# screen controllers (e.g., SmithScreenController, InventoryScreenController) that dynamically instantiate full SubViews or Panels via .Instantiate(), you must explicitly apply style.flexGrow = 1 to the spawned instance before adding it to the parent container. Note: Do not apply this to small, fixed-size atomic components like individual item slots.

6. Third-Party Plugin Isolation

    The Rule: Never modify the source .uxml or .uss files of third-party assets (e.g., external Dialogue Systems).

    Execution: Treat third-party UI files as read-only. To alter their appearance, create a custom .uss file in the project's own UI directory, use the UI Debugger to identify the target classes, write override rules, and attach that custom stylesheet to the root UI Document.