# Core Inventory and UI Architecture

This document serves as the master reference for the Item, Inventory, and UI systems in Outland Haven. It consolidates technical documentation with a deep-dive analysis of the actual codebase, explaining not just *what* exists, but *how* the various scripts interact to create a cohesive system.

---

## 1. Architectural Overview & Patterns

The system is built on a strict **Model-View-Presenter (MVP)** pattern, heavily leveraging **ScriptableObject-based Event Channels** to maintain decoupling. This approach solves the common Unity problem of "spaghetti code" where UI elements are tightly coupled to game state.

### 1.1 The MVP Framework in Action
*   **Model (The State & Data)**:
    *   `InventoryManager`: The primary container for items. It is a `MonoBehaviour` but behaves like a pure data store.
    *   `ShopManagerSO`, `CraftingManagerSO`: ScriptableObject managers that handle business logic. They are "Models" in the sense that they own the rules of transaction and transformation.
*   **View (The Presentation Layer)**:
    *   `UIView` / `GameView`: Base classes that wrap the UI Toolkit's `VisualElement` hierarchy.
    *   `PlayerInventoryView`, `ShopSubView`: Specific implementations that map backend data to the screen.
    *   `InventorySlotView`: A granular view class that manages a single slot's visual state and handles raw pointer events.
*   **Presenter / Controller (The Orchestrator)**:
    *   `InventoryScreenController`, `SmithScreenController`: These are the "glue." They live in the Unity scene, hold references to both the Models (Managers) and the UXML templates, and instantiate the Views. They inject the necessary dependencies into the Views during `Initialize()`.

### 1.2 Data Flow & Event Synchronization
Synchronization between the data (Model) and the screen (View) is entirely event-driven, using **`UIInventoryEventsSO`** as a global bus.

1.  **Intent**: A user interacts with a View (e.g., Right-Clicking a potion).
2.  **Request**: The View broadcasts a semantic intent (e.g., `OnRequestUse.Invoke(slot)`).
3.  **Validation & Execution**: A specialized controller (like `InventoryActionController`) or manager listens for the request, validates the game rules, and modifies the data in the `InventoryManager`.
4.  **Notification**: Upon modification, the `InventoryManager` or Manager SO triggers a response event (e.g., `OnSpecificSlotsUpdated`).
5.  **Reaction**: Any active View (like `PlayerInventoryView` or `HUDView`) listening to that event updates its visual representation.

---

## 2. Item Components & Inventory Logic

### 2.1 Blueprint/State Pattern: Decoupling Definitions from Data
The item system is designed for high extensibility using a modular approach.
*   **`InventoryItemSO` (The Blueprint)**: This is a static asset created in the Editor. It defines the "what" (Icon, Name, Max Stack). It holds a list of `ItemComponent` objects.
*   **`ItemComponent` (The Behavior Definition)**: Abstract classes like `EquipableComponent` or `ConsumableComponent`. These define the rules.
*   **`ItemInstance` (The Runtime Object)**: When an item is created in-game, it is wrapped in an `ItemInstance`. This class holds a `Guid` for persistence and a list of `ItemComponentState` objects.
*   **`ItemComponentState` (The Live Data)**: This is where mutable data lives (e.g., `ConsumableState.CurrentCharges`).
    *   **Logic Example**: `ItemInstance.IsStackableWith(other)` doesn't just check the item type; it iterates through every `ItemComponentState` and asks them if they are compatible. This is why two identical swords with different durability or levels might not stack.

### 2.2 Inventory Management Scripts
*   **`InventoryManager.cs`**: Handles the authoritative `AddItem` and `RemoveItem` logic. It is responsible for finding empty slots or existing stacks and cloning `ItemInstance` objects to prevent shared reference bugs.
*   **`InventorySlot.cs`**: A pure C# class representing a single storage unit. It includes the `CanAccept(ItemInstance)` method, which uses the `SlotFilterType` to enforce equipment restrictions.
*   **`InventoryTransferManagerSO.cs`**: A critical ScriptableObject that handles the logic of moving items between containers. It acts as a "Banker," ensuring that if a swap fails (e.g., trying to put a weapon in a potion slot), the items remain safely in their original locations.

---

## 3. UI Runtime Interactions: The "Dumb" View Pattern

### 3.1 Drag and Drop Mechanics in `InventorySlotView.cs`
The `InventorySlotView` is one of the most complex scripts in the UI system because it must translate raw pointer movements into high-level game actions.

1.  **Pointer Capture**: On `PointerDown`, the slot captures the pointer to ensure it receives all subsequent move/up events even if the cursor leaves the slot's bounds.
2.  **The Threshold**: Dragging doesn't start instantly. It waits for a 10px move threshold to prevent accidental drags during simple clicks.
3.  **Visual Ghosting**: Once the threshold is met, the view broadcasts `OnGlobalDragStarted`. The **`UIDragManager.cs`** (a scene singleton) listens to this to create and move a floating icon following the cursor.
4.  **Raycast Resolution**: On `PointerUp`, the script uses `panel.Pick(evt.position)`. Because UI Toolkit hierarchies can be deep, the script uses a recursive **`FindTargetDropData`** helper to climb the parent tree until it finds a `VisualElement` with valid `userData` (either `SlotDropData` or a proxy ID).

### 3.2 Context-Sensitive Interactions
The system uses the **`InventoryInteractionContext`** enum to redefine what a right-click does without changing the underlying View code.
*   **Script Role**: `PlayerInventoryView` listens to `OnInteractionContextChanged`.
    *   If the context is `Shop`, a right-click invokes `OnRequestSell`.
    *   If the context is `Normal`, it checks the item's components: if it has an `EquipableComponent`, it fires `OnRequestEquip`.

---

## 4. Undiscovered / Unmentioned Functionalities

Analysis of the scripts reveals several systems that are implemented but may not be immediately obvious in the UI:

*   **`EvolvingItemModule.cs`**: Implements a "Kill Tracker" for weapons. The `EvolvingState` tracks `CurrentKills` and `IsAwakened`. Once the `KillsRequired` limit is reached (defined in the `EvolvingComponent`), the weapon is flagged as awakened.
*   **`UpgradeableModule.cs`**: A generic system for item leveling. It prevents items of different levels from stacking and provides a foundation for a blacksmith upgrade system.
*   **Proxy Slot Processing**: In `ForgeSubView.cs` and `SalvageSubView.cs`, items are placed into "Proxy Slots." These slots are visually independent of the player's inventory. The `userData` of these visual elements is set to a string (e.g., `"forge-slot-1"`), which tells the drag-and-drop system to trigger a "processing request" rather than a standard "move request."
*   **Auto-Resolution (`PlayerInventorySceneResolver.cs`)**: A utility used by `InventoryActionController` to dynamically find the player's inventory and equipment containers at runtime if they weren't manually assigned in the Inspector.

---

## 5. Critical Analysis (Flaws, Bugs, and Patterns)

### 5.1 Good Patterns (The "Wins")
*   **Targeted Redraws**: By using `OnSpecificSlotsUpdated(source, target)`, the UI avoids re-instantiating dozens of `VisualElement`s during a simple item move. This keeps the UI responsive even with large inventories.
*   **Strict MVP Separation**: The fact that `InventoryManager` can exist and function perfectly in a scene without any UI at all is a testament to the architecture's modularity.
*   **Modular Component States**: Adding a new feature (like "Cursed Items" or "Gem Sockets") only requires creating a new `ItemComponent` and `ItemComponentState`, leaving the core inventory logic untouched.

### 5.2 Bad Patterns & Technical Debt (The "Flaws")
*   **Hardcoded Index Dependencies**: The `PlayerEquipmentController` assumes a rigid array structure (0=Head, 1=Chest, etc.). This makes the system fragile; adding a "Ring" slot would require updating hardcoded integers across multiple scripts (`PlayerEquipmentController`, `InventoryActionController`).
*   **String-Based Identification**: `InventoryManager` identifies the player's equipment container by checking if the GameObject name contains "Equip". This is an anti-pattern that relies on scene naming conventions rather than explicit references or robust tags.
*   **Input System Fragmentation**: The project is in a transitional state between the old Unity Input Manager and the new Input System. `InventorySlotView` uses `evt.shiftKey` (UI Toolkit), while `ShopSubView` uses `Input.GetKey` (Legacy), and other parts of the game use `InputSystem_Actions`.

### 5.3 Known Bugs & Unhandled Edge Cases
*   **The "Void" Drop**: If a player drops an item into the empty space between UI windows, `panel.Pick` returns null. Currently, the drag simply stops. There is no logic to "drop the item on the ground" in the 3D world or to "return to sender" in a way that provides clear feedback to the player.
*   **Partial Stack Swap Logic**: The `InventoryTransferManagerSO` prevents swapping if the `amountToMove` is less than the full stack. While this avoids complex "split-and-swap" math, it can feel like a bug to players who expect the UI to handle the logic for them.
*   **Missing UI Feedback for Locked Slots**: While `InventorySlot.CanAccept` correctly prevents putting a potion in a sword slot, the UI provides no visual feedback (like a red highlight) *during* the drag to show which slots are valid targets.
*   **Serialization Risks**: `ItemInstance` uses `[SerializeReference]` for its states. While powerful, this can lead to "Missing types" errors if classes are renamed or moved between namespaces without proper migration.
