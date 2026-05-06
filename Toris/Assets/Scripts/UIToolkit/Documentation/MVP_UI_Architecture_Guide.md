# MVP UI Architecture Guide

This document provides a technical guide to the C# UI implementation of Outland Haven, detailing how the project utilizes the Model-View-Presenter (MVP) pattern with Unity's UI Toolkit.

## 1. MVP Framework Application

The UI system enforces a strict Model-View-Presenter (MVP) pattern to cleanly decouple presentation from game logic and state.

*   **Model:** The source of truth for data and state (e.g., `InventoryManager`, `ShopManagerSO`, `PlayerProgression`). Models are strictly responsible for data validation, storage, and broadcasting changes via events. They have no knowledge of the UI.
*   **View:** The presentation layer (`UIView`, `GameView`). Views are entirely "dumb" pure C# classes. They construct the visual hierarchy via UI Toolkit's UXML, map raw data to `VisualElement`s, and capture hardware inputs. Views never hold game logic or process transactions.
*   **Presenter (Controller):** The orchestrator (`MonoBehaviour`s like `InventoryScreenController`). Presenters act as the bridge between Unity's scene lifecycle, the backend Models, and the pure C# Views. They instantiate templates, resolve dependencies, inject data into the Views, and map global systems.

## 2. Presenters (Controllers): Managing Logic and State

Controllers manage the initialization and context of their respective UI modules, hooking them into the global `UIManager`.

### 2.1 The Inventory Module (`InventoryScreenController`)
*   **Initialization:** During `Start()`, the controller instantiates the UXML templates (`InventoryMainTemplate`, `SlotTemplate`).
*   **Dependency Injection:** It constructs the `PlayerInventoryView`, passing in necessary ScriptableObject event channels (`UIEventsSO`, `UIInventoryEventsSO`), physical data containers (`EquipmentInventory`, `PotionInventory`), and the `PlayerHUDBridge` to act as the single source of truth for player stats.
*   **Registration:** It registers the initialized View with the central `UIManager`, specifying its layout constraint (`ScreenZone.Right`).

### 2.2 The Smithy Module (`SmithScreenController`)
*   **Complex Orchestration:** Manages a composite UI containing multiple sub-views (`ShopSubView`, `ForgeSubView`, `SalvageSubView`). It injects highly specialized managers (`ShopManagerSO`, `CraftingManagerSO`, `SalvageManagerSO`) into the `SmithView`.
*   **Contextual Setup:** Subscribes to `UIEvents.OnRequestOpen`. When a request to open the Smith screen occurs, the Controller intercepts the payload (e.g., a specific vendor's `InventoryManager`) and injects it into the `ShopManagerSO` before the View renders, ensuring the UI always displays the correct contextual data.

## 3. View Hierarchy: `UIView` and `GameView`

The visual layer is built upon an object-oriented inheritance structure, separating generic UI functionality from screen-level management.

### 3.1 `UIView` (The Base Unit)
*   **Purpose:** Represents any functional block of UI, from a single button to a complex panel.
*   **Visual Binding:** Wraps a root `VisualElement` (passed via the constructor) and manages finding specific child elements within the DOM hierarchy (e.g., using `root.Q<VisualElement>("ElementName")`).
*   **Core Methods:** Provides virtual methods for lifecycle: `Initialize()` (sets up elements and callbacks), `Setup(object payload)` (injects data before display), `Show()`, and `Hide()` (toggles CSS `display` properties).

### 3.2 `GameView` (The Screen Unit)
*   **Purpose:** An abstract class extending `UIView`, designed to act as a top-level screen managed by the `UIManager`.
*   **Identity:** Enforces an abstract `ScreenType ID` property (e.g., `ScreenType.Inventory`, `ScreenType.HUD`).
*   **Event Integration:** Overrides `Show()` and `Hide()` to not only toggle visibility but to broadcast generic structural events (`UIEvents.OnScreenOpen` and `OnScreenClose`), allowing global systems to react when a major screen changes state.

## 4. Lifecycle & Cleanup (`IDisposable`)

Because Views are pure C# classes operating alongside Unity's garbage-collected environment, manual resource management is required to prevent memory leaks and ghost event firing.

*   **Implementation:** `UIView` implements the `IDisposable` interface.
*   **Subscription:** When a View is initialized (`Initialize()`), it typically subscribes to local UI Toolkit events (e.g., `ClickEvent`) and global ScriptableObject events (e.g., `OnSpecificSlotsUpdated`).
*   **Disposal:** The `Dispose()` method must be overridden in child classes. When the UI is destroyed or completely rebuilt, `Dispose()` is called to explicitly unregister all callbacks and detach event listeners from global channels. Failure to do so results in null reference exceptions when backend systems try to update a destroyed UI element.

## 5. Data Flow (Model -> Presenter -> View)

The UI adheres strictly to a unidirectional, event-driven data flow.

1.  **State Mutation (Model):** An action occurs in the backend (e.g., `InventoryTransferManagerSO` successfully moves an item). The Model updates its internal state.
2.  **Event Emission:** The Model or Manager broadcasts a notification via a ScriptableObject event channel (e.g., `UIInventoryEventsSO.OnSpecificSlotsUpdated(source, target)`).
3.  **View Reception:** The View (having subscribed during initialization via the Presenter) receives the event. Note: Views receive simple data structs or notifications, keeping them ignorant of business logic.
4.  **Visual Update:** The View updates the specific `VisualElement`s involved (e.g., redrawing the icon and quantity of the two swapped slots).
5.  **User Input (View):** A user performs an action (e.g., clicking a "Sell" button).
6.  **Intent Broadcast:** The View translates the hardware UI Toolkit event into a generic semantic intent and broadcasts it (e.g., `UIInventoryEventsSO.OnRequestSell.Invoke(slotData)`). It does *not* modify the inventory itself.
7.  **Resolution:** The backend System Manager listens to this intent request, validates it against game rules, and if successful, triggers step 1, restarting the cycle.
