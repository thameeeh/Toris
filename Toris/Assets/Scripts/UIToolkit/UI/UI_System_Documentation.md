# UI System Documentation

This document outlines the architecture and conventions for the UI system used in this project, which leverages Unity's UI Toolkit.

## Architecture Overview

The UI system is designed with a clear separation of concerns, divided into Controllers, Views, and a central Manager, communicating via an Event System.

*   **Manager (`UIManager`):** The central hub that manages the lifecycle and layout zones of all registered UI screens. It listens to global UI events to open, close, and toggle screens.
*   **Views (`UIView`, `GameView`):** Represent the actual UI components. They handle internal logic, data binding, and visual state management (Show/Hide).
*   **Controllers (e.g., `HudScreenController`):** MonoBehaviour components responsible for dependency injection. They load UXML templates (`VisualTreeAsset`), instantiate them, create the corresponding View, and register it with the `UIManager`.
*   **Event System (`UIEventsSO`):** A ScriptableObject-based event bus that decouples UI components from each other and from gameplay systems.

## Core Components

### 1. The Manager (`UIManager.cs`)

The `UIManager` is attached to a `GameObject` that also contains the root `UIDocument`. Its primary responsibilities are:

*   **Zone Management:** It queries the root `UIDocument` for specific layout zones (e.g., `Layer_HUD`, `Left_Zone`, `Right_Zone`).
*   **View Registration:** It accepts `GameView` instances via the `RegisterView(GameView view, ScreenZone zone)` method and appends their root `VisualElement` to the specified zone.
*   **Event Handling:** It listens to `UIEventsSO` (like `OnRequestOpen`, `OnRequestClose`) and manages the visibility of registered views based on their `ScreenType`.

### 2. Screen Types & Zones (`ScreenTypes.cs`)

To manage different UI windows effectively, the system uses two main enums:

*   `ScreenType`: Identifies the specific functional screen (e.g., `HUD`, `Inventory`, `CharacterSheet`, `PauseMenu`, `Smith`). This is used for event payloads to open/close specific screens.
*   `ScreenZone`: Defines *where* a screen should be placed within the main `UIDocument` layout (e.g., `HUD`, `Left`, `Right`, `Modal`). This allows multiple screens to be open simultaneously in different areas.

### 3. Views (`UIView.cs`, `GameView.cs`)

Views encapsulate the logic and visual representation of a UI element.

*   `UIView`: The base class. It holds the root `VisualElement`, handles basic visibility (`Show()`, `Hide()`), and provides virtual methods for initialization (`SetVisualElements`, `RegisterButtonCallbacks`).
*   `GameView`: Inherits from `UIView`. It adds a `ScreenType ID` property to uniquely identify the view type and holds a reference to `UIEventsSO`. It also provides a `Setup(object payload)` method for passing contextual data when opening a screen. Specific screens (like `HUDView`, `PlayerInventoryView`) inherit from `GameView`.

### 4. Controllers (e.g., `InventoryScreenController.cs`)

Controllers act as the bridge between Unity's Component system (MonoBehaviours, inspector references) and the plain C# View classes.

Their workflow is generally:
1.  Receive dependencies via the Inspector (e.g., `VisualTreeAsset` for the main template and sub-templates, Data ScriptableObjects, Event ScriptableObjects).
2.  In `OnEnable()`, instantiate the main `VisualTreeAsset` into a `TemplateContainer`.
3.  Instantiate the specific `GameView` class, passing in the `TemplateContainer` and other necessary data.
4.  Find the `UIManager` in the scene.
5.  Call `_uiManager.RegisterView(_view, ScreenZone.TargetZone)`.

### 5. Event System (`UIEventsSO.cs`)

The system relies heavily on `ScriptableObject`-based events (`UIEventsSO`) to avoid tight coupling.

*   `OnRequestOpen(ScreenType, object payload)`: Fired when a system (e.g., player input, an NPC interaction) wants to open a screen. The payload can pass contextual data (like a vendor's inventory).
*   `OnRequestClose(ScreenType)`: Fired to close a specific screen.
*   `OnRequestCloseAll()`: Closes all non-HUD screens.
*   `OnScreenOpen(ScreenType)`: Fired by a `GameView` when it successfully opens, allowing other systems to react.

## Best Practices & Rules

When working with or extending the UI system, adhere to the following rules:

1.  **NO `UIDocument` in Controllers:** Do not use or search for `UIDocument` components in individual screen Controllers. The only script that should reference a `UIDocument` is the root `UIManager`.
2.  **Use `VisualTreeAsset`:** Controllers must require a `VisualTreeAsset` (UXML) via the Inspector. Instantiate this asset (`asset.Instantiate()`), pass the resulting `TemplateContainer` instance to the View, and register the view with the `UIManager` specifying the appropriate `ScreenZone`.
3.  **Prefer USS over C# Inline Styles:** When modifying UI Toolkit layouts, always prefer adjusting the UXML or USS files for layout, sizing, and styling. **Avoid using C# inline styles** (e.g., `element.style.width = ...`). C# inline styles override all other stylesheets and can cause unintended project-wide regressions or make maintenance difficult. Handle state changes (like showing/hiding) via the provided View methods (`Show()` / `Hide()`) which modify `display` safely, or by toggling USS classes.
4.  **Decouple via Events:** Do not have Views or Controllers directly reference each other to open screens. Always use the `UIEventsSO` to request opening or closing screens.
