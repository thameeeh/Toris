# Scripts Architecture Overview

This document consolidates and compresses the technical descriptions of the various scripts driving the UI Toolkit, Inventory, and Item systems within Outland Haven. Instead of individual file breakdowns, scripts are grouped by their architectural responsibilities to provide a concise, high-level map of the codebase.

## 1. Global Event Channels (ScriptableObjects)

The architecture heavily relies on the Observer pattern to decouple UI from game logic. This is achieved through `ScriptableObject` Event Buses that define cross-system intents.

*   **`UIEventsSO`**: Manages broad window state requests (`OnRequestOpen`, `OnRequestCloseAll`) without caring about the specific screen's content.
*   **`UIInventoryEventsSO`**: The central nervous system for all item transactions. It handles drag-and-drop visuals (`OnGlobalDragStarted`), localized redraws (`OnSpecificSlotsUpdated`), and semantic gameplay requests (`OnRequestBuy`, `OnRequestEquip`, `OnRequestMoveItem`).
*   **`UISkillEventsSO`**: Decouples the skill tree UI from progression data by broadcasting skill unlock requests and state changes.

## 2. Core Data Containers & Models

These scripts represent the pure data layer. They contain no UI rendering logic and serve as the source of truth for items and inventories.

*   **The Blueprint System (`InventoryItemSO`, `ItemComponent`, `ItemComponentState`)**: Implements the Open-Closed Principle. `InventoryItemSO` is static read-only data. `ItemComponent` definitions (e.g., `EquipableModule`, `ConsumableModule`, `OffensiveModule`) describe behaviors. When instantiated into an `ItemInstance` at runtime, these components generate `ItemComponentState` to hold mutable data (like durability or charges).
*   **Inventory Storage (`InventoryManager`, `InventorySlot`)**: Generic, reusable storage. `InventoryManager` handles the high-level list of `InventorySlot` data structures. `InventorySlot` manages an `ItemInstance` and a quantity count, utilizing `SlotFilterType` enums to restrict what items it can hold (e.g., specifically accepting Weapons or Consumables).
*   **World Anchors (`PlayerHUDBridge`, `GameSessionSO`)**: `GameSessionSO` acts as a global entry point for player progression data (inventory, skills). `PlayerHUDBridge` is the critical translation layer that listens to complex backend MonoBehaviours (like `PlayerStats`) and re-emits them as simple generic C# events (like `OnHealthChanged`) for the UI to consume safely.

## 3. The UI Presentation Layer (Views)

These are pure C# classes responsible for wrapping UXML layouts, handling raw pointer inputs, and updating visual states. They are deliberately "dumb" and contain no game logic.

*   **Base UI Templates (`UIView`, `GameView`)**: Abstract bases providing standardized methods for `Initialize()`, `Show()`, and `Hide()`, enforcing strict `IDisposable` event unsubscription to prevent memory leaks.
*   **Slot Micro-Views (`InventorySlotView`)**: The lowest-level visual wrapper for an item slot. Handles native UI Toolkit pointer events, calculates drag thresholds, and maps hardware inputs to local C# delegates without understanding game context.
*   **Grid & Screen Views (`PlayerInventoryView`, `PlayerEquipmentView`, `ShopSubView`, `SalvageSubView`, `SmithView`, `PlayerSkillView`)**: These act as translators. They manage collections of slots (like a grid of `InventorySlotView`s). They listen to raw slot clicks and translate them into global semantic intents based on current contexts (e.g., translating a right-click into "Sell" if the shop is open, or "Unequip" if in the equipment window).

## 4. The Logic & Presenter Layer (Managers & Controllers)

These MonoBehaviours and ScriptableObjects act as the mediators. They listen to the intents broadcast by the Event Channels and mutate the Core Data Models.

*   **Screen Controllers (`InventoryScreenController`, `HudScreenController`, `MainMenuScreenController`)**: MonoBehaviours placed in the Unity scene. They instantiate the raw `Views`, inject necessary data dependencies (like `InventoryManager` or `PlayerHUDBridge`), and map input actions (like pressing the 'I' key) to open/close requests.
*   **Transaction Managers (`InventoryTransferManagerSO`, `ShopManagerSO`, `SalvageManagerSO`, `CraftingManagerSO`)**: The authoritative banks. They intercept semantic UI requests (`OnRequestMoveItem`, `OnRequestBuy`). They validate the action against the rules (e.g., checking if the player has enough gold, or if an item can mathematically fit in a stack), execute the data mutation, and then fire targeted update events back to the UI.
*   **Player Executors (`InventoryActionController`, `SkillManager`)**: These scripts bridge the inventory/skill data directly to the player's physical avatar. They catch usage requests (`OnRequestEquip`, `OnRequestUse`, `OnRequestUnlockSkill`) and apply the actual stat modifications or visual equipment changes to the character entity.

## 5. Auxiliary Architecture Systems

*   **The Drag-and-Drop Coordinator (`UIDragManager`)**: Purely handles the absolute visual representation of a dragging item. It tracks the mouse pointer to render the ghost icon, completely separated from the data-moving logic of the `InventoryTransferManagerSO`.
*   **Bootstrapping & World Interaction (`SystemBootstrapper`, `WorldContainer`, `WorldItem`, `ItemPickEventSO`)**: Handles initialization of core services and the physical representation of items in the Overworld. `WorldItem`s drop into the physical scene, and picking them up fires a generic `ItemPickEventSO` that the inventory backend listens for to absorb the item.
