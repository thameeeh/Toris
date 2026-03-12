# UI and Inventory Timing Implementation Documentation

This document summarizes the usage of Unity lifecycle timing methods (`Awake`, `OnEnable`, `Start`) across the UI and inventory-related scripts in the project. Proper understanding of these timing methods is crucial for correct initialization, dependency injection, and event subscription, especially within the UI Toolkit and ScriptableObject architectures.

## Overview of Timing Usage

The UI and inventory systems generally follow these conventions:
*   **`Awake()`**: Used for early initialization, specifically for finding local components (like `UIDocument`) or global managers (like `UIManager` via `FindFirstObjectByType`). It is also used to initialize singletons or core dependencies before other objects might need them.
*   **`OnEnable()`**: Used primarily for validation (checking for missing serialized references like templates or ScriptableObjects), finding UI elements within a parsed `UIDocument`, and subscribing to events (like UI events or input actions).
*   **`Start()`**: Used for assembling the UI, instantiating templates, creating View instances (passing instantiated containers and dependencies), and registering those Views with the central `UIManager`.

---

## Detailed Script Analysis

### 1. Controllers (UI Toolkit)

Controllers act as the bridge between Unity's lifecycle/scene and the pure C# UI Views.

#### `UIManager` (`Toris/Assets/Scripts/UIToolkit/UI/UIViews/UIManager.cs`)
*   **`Awake()`**:
    *   Retrieves the root `VisualElement` from the attached `UIDocument`.
    *   Queries for specific layout zones (`Layer_HUD`, `Left_Zone`, `Right_Zone`).
    *   Logs an error if essential zones are missing.
    *   *Why here?* To guarantee layout zones are ready before any `ScreenController` attempts to register a view in `Start()`.
*   **`OnEnable()`**:
    *   Subscribes to global UI events (`_UIEvents.OnRequestOpen`, `OnRequestClose`, `OnRequestCloseAll`).
    *   *Why here?* Standard Unity practice to balance event subscriptions with `OnDisable()`.

#### `HudScreenController` (`Toris/Assets/Scripts/UIToolkit/UI/Controllers/HudScreenController.cs`)
*   **`Awake()`**:
    *   Finds and caches the `UIManager` using `FindFirstObjectByType<UIManager>()`.
*   **`OnEnable()`**:
    *   Validates the presence of `_hudMainTemplate`.
    *   Instantiates the HUD template container.
    *   Constructs the `HUDView` with dependencies.
    *   Initializes the view and registers it with the `UIManager` (`ScreenZone.HUD`).
    *   *Why here?* Delaying instantiation to `Start()` ensures `UIManager` has fully executed its `Awake()` and is ready to accept registrations.

#### `InventoryScreenController` (`Toris/Assets/Scripts/UIToolkit/UI/Controllers/InventoryScreenController.cs`)
*   **`Awake()`**:
    *   Finds and caches the `UIManager`.
*   **`OnEnable()`**:
    *   Validates the presence of `_inventoryMainTemplate`.
*   **`Start()`**:
    *   Instantiates the inventory template container.
    *   Constructs the `PlayerInventoryView` and explicitly calls `_view.Initialize()`.
    *   Registers the view with `UIManager` (`ScreenZone.Right`).
    *   *Why here?* Delaying instantiation to `Start()` ensures `UIManager` has fully executed its `Awake()` and is ready to accept registrations.

#### `SmithScreenController` (`Toris/Assets/Scripts/UIToolkit/UI/Controllers/SmithScreenController.cs`)
*   **`Awake()`**:
    *   Finds and caches the `UIManager`.
    *   Calls `Initialize()` on the injected `ShopManagerSO` (`_shopManagerSO`).
*   **`OnEnable()`**:
    *   Validates the presence of required templates (`_smithMainTemplate`, `_slotTemplate`).
*   **`Start()`**:
    *   Instantiates the main smith template.
    *   Constructs the `SmithView` and explicitly calls `_view.Initialize()`.
    *   Registers the view with `UIManager` (`ScreenZone.Left`).

#### `MainMenuScreenController` (`Toris/Assets/Scripts/UIToolkit/UI/Controllers/MainMenuScreenController.cs`)
*   **`OnEnable()`**:
    *   Gets the `UIDocument` component.
    *   Queries for interactive buttons (`btn-start-game`, `btn-quit-game`).
    *   Subscribes to button `clicked` events.
    *   *Note:* It lacks `OnDisable` for unsubscription, which is generally safe for Main Menu but good practice to include.

### 2. ScriptableObjects (Data & Events)

#### `InventoryContainerSO` (`Toris/Assets/Scripts/UIToolkit/ScritableObjects/InventoryContainerSO.cs`)
*   **`OnEnable()`**:
    *   Contains commented/stub logic for initializing the list.
    *   *Note on SOs:* `OnEnable` in ScriptableObjects is called when the SO is loaded (editor domain reload or runtime initialization). It's used to reset or initialize transient state that shouldn't persist between sessions.

#### `PlayerDataSO` (`Toris/Assets/Scripts/UIToolkit/ScritableObjects/PlayerDataSO.cs`)
*   **`OnEnable()`**:
    *   Contains logic to reset state when the game starts or the SO loads.

---

## Recommendations

1.  **Consistent Registration Timing:** All UI Toolkit controllers use `Start()` to instantiate templates and register with the `UIManager` (e.g., `InventoryScreenController`, `SmithScreenController`, `HudScreenController`). This guarantees `UIManager.Awake()` has executed and prevents race conditions during scene load. Ensure new controllers follow this pattern.
2.  **View Initialization:** As per project guidelines, UI Toolkit Views (e.g., `ShopSubView`) should continue to be correctly initialized by calling an explicit `Initialize()` method from the Controller after instantiation, avoiding work in the View constructor.
3.  **Event Unsubscription:** While `OnEnable` is heavily used to subscribe to UI events, ensure corresponding `OnDisable` methods are consistently implemented across all scripts to prevent memory leaks or multiple subscriptions.
4.  **Component Retrieval:** Continue using `TryGetComponent<T>(out var component)` instead of `GetComponent<T>()` and null checks, especially in update loops or physics callbacks, to avoid unnecessary memory allocations.
