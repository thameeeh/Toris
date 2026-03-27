# UI Architecture Documentation

This document outlines the architecture and conventions for the UI system used in this project, which leverages Unity's UI Toolkit.

## Architecture Overview

The UI system is designed with a clear separation of concerns, divided into Controllers, Views, and a central Manager, communicating via an Event System.

It heavily enforces an MVP (Model-View-Presenter) pattern where Views are "dumb" and Logic resides in Presenters/Managers.

## Core Components

### 1. The Manager (`UIManager.cs`)

The `UIManager` is attached to a `GameObject` that also contains the root `UIDocument`. Its primary responsibilities are:

*   **Location:** `Toris/Assets/Scripts/UIToolkit/UI/UIViews/UIManager.cs`
*   **Zone Management:** It queries the root `UIDocument` for specific layout zones (e.g., `Layer_HUD`, `Left_Zone`, `Right_Zone`).
*   **View Registration:** It accepts `GameView` instances via the `RegisterView(GameView view, ScreenZone zone)` method and appends their root `VisualElement` to the specified zone.
*   **Mutual Exclusivity:** `UIManager` enforces mutual exclusivity for views sharing the same `ScreenZone` (excluding the `HUD` zone). Opening a new view automatically closes any currently active view in that same zone.

### 2. Screen Types & Zones (`ScreenTypes.cs`)

To manage different UI windows effectively, the system uses two main enums:

*   **Location:** `Toris/Assets/Scripts/UIToolkit/UI/Events/ScreenTypes.cs`
*   `ScreenType`: Identifies the specific functional screen (e.g., `HUD`, `Inventory`, `CharacterSheet`, `PauseMenu`, `Smith`). This is used for event payloads to open/close specific screens.
*   `ScreenZone`: Defines *where* a screen should be placed within the main `UIDocument` layout (e.g., `HUD`, `Left`, `Right`, `Modal`). This allows multiple screens to be open simultaneously in different areas.

### 3. Views (`UIView.cs`, `GameView.cs`)

Views encapsulate the logic and visual representation of a UI element.

*   `UIView`: The base class. It holds the root `VisualElement`, handles basic visibility (`Show()`, `Hide()`), and provides virtual methods for initialization (`SetVisualElements`, `RegisterButtonCallbacks`).
*   `GameView`: Inherits from `UIView`. It adds a `ScreenType ID` property to uniquely identify the view type and holds a reference to `UIEventsSO`. It also provides a `Setup(object payload)` method for passing contextual data when opening a screen. Specific screens (like `HUDView`, `PlayerInventoryView`) inherit from `GameView`.
*   **Important Usage**: When programmatically opening dependent UI views, always explicitly call `view.Setup(payload)` before `view.Show()`. `Show()` only modifies display visibility; failing to call `Setup()` will result in an uninitialized, empty UI.

### 4. SubViews and Nested UI

When implementing tabbed UI screens (e.g., Mage, Smith) with nested functionality (like Shops or Forges):
*   The main ScreenController must serialize and pass the required sub-view `VisualTreeAsset` templates (e.g., `_shopTemplate`, `_slotTemplate`) to the main View.
*   The View is responsible for lazily instantiating these templates into SubView classes (e.g., `ShopSubView`) upon tab selection, and handling their `Hide()` and `Dispose()` lifecycles.

### 5. Controllers (e.g., `InventoryScreenController.cs`)

Controllers act as the bridge between Unity's Component system (MonoBehaviours, inspector references) and the plain C# View classes.

Their workflow is generally:
1.  Receive dependencies via the Inspector (e.g., `VisualTreeAsset` for the main template and sub-templates, Data ScriptableObjects, Event ScriptableObjects).
2.  In `OnEnable()`, validate the required templates or references.
3.  In `Start()`, instantiate the main `VisualTreeAsset` into a `TemplateContainer`.
4.  Instantiate the specific `GameView` class, passing in the `TemplateContainer` and other necessary data.
5.  Find the `UIManager` in the scene (typically in `Awake()`).
6.  Call `_uiManager.RegisterView(_view, ScreenZone.TargetZone)`.

## Best Practices & Rules

1.  **NO `UIDocument` in Controllers:** Do not use or search for `UIDocument` components in individual screen Controllers. The only script that should reference a `UIDocument` is the root `UIManager`.
2.  **Use `VisualTreeAsset`:** Controllers must require a `VisualTreeAsset` (UXML) via the Inspector. Instantiate this asset (`asset.Instantiate()`), pass the resulting `TemplateContainer` instance to the View, and register the view with the `UIManager` specifying the appropriate `ScreenZone`.
3.  **Prefer USS over C# Inline Styles:** When modifying UI Toolkit layouts, always prefer adjusting the UXML or USS files for layout, sizing, and styling. **Avoid using C# inline styles** (e.g., `element.style.width = ...`). C# inline styles override all other stylesheets and can cause unintended project-wide regressions or make maintenance difficult. Handle state changes (like showing/hiding) via the provided View methods (`Show()` / `Hide()`) which modify `display` safely, or by toggling USS classes.
4.  **Decouple via Events:** Do not have Views or Controllers directly reference each other to open screens. Always use the `UIEventsSO` to request opening or closing screens.
5.  **Dumb Views:** Do not pass `GameSessionSO` or `InventoryManager` directly to UI SubViews for logic checks. Views should only query managers or listen for events. Validation logic must reside in Manager/Presenter classes (e.g., `CraftingManagerSO`, `SalvageManagerSO`).

## Recommendations

*   **View Construction & Logic:** Maintain the principle that logic should be separated from presentation. Views should solely update visuals based on events and shouldn't handle game state changes like directly modifying item counts in the inventory container.
*   **Controller Flow Consistency:** All controllers should execute `_uiManager.RegisterView` inside `Start()` instead of `OnEnable()` to avoid race conditions with the `UIManager`'s own `Awake()` method and zone finding logic.
*   **Cleanup and Event Management:** All views and controllers should ensure proper event cleanup. When `Hide()` is called on a view, it should unbind visual-specific events to prevent memory leaks if the controller disables.
