# Script Dependency Documentation

This document outlines the broad architectural dependencies between the UI, Inventory, and Item systems within Outland Haven. It highlights the primary data flow, component ownership, and event-driven communication pathways.

## Core UI Architecture

The UI utilizes a Model-View-Presenter (MVP) inspired pattern, coordinated by the `UIManager`.

**Dependency Chain:**
**ScreenController** -> **GameView** -> **UIManager** -> **VisualElement**

*   **ScreenController** (e.g., `InventoryScreenController`) instantiates UI Toolkit templates, injects necessary ScriptableObject dependencies (`GameSessionSO`, `UIEventsSO`, `UIInventoryEventsSO`), and initializes the **GameView**.
*   **GameView** (e.g., `PlayerInventoryView`) handles the actual visual updates and user interactions.
*   **ScreenController** registers the initialized view with the **UIManager** for a specific `ScreenZone`.
*   **UIManager** manages the lifecycle and screen zones (HUD, Left, Right) for all view instances and appends them to the root **VisualElement**.

*See `UI_Architecture_Documentation.md` for more details.*

## Inventory & Item System

The inventory system relies heavily on ScriptableObjects for flyweight data and dynamic instances for runtime state.

**Dependency Chain:**
**InventoryManager** -> **InventorySlot** -> **ItemInstance** -> **InventoryItemSO** -> **ItemComponent**

*   **InventoryManager** (MonoBehaviour) reads the `InventoryContainerSO` blueprint, registers itself globally to `GameSessionSO.PlayerInventory` (if applicable), and manages a list of live runtime **InventorySlot**s.
*   **InventorySlot** holds runtime **ItemInstance** wrappers containing quantity and state.
*   **ItemInstance** references the base **InventoryItemSO** blueprint for immutable data and holds a list of dynamically generated **ItemComponentState**s.
*   **InventoryItemSO** defines the modular behaviors (**ItemComponent** like `EquipableComponent`) which spawn states via a Factory Method.

*See `Inventory_Management_Documentation.md` and `Item_Architecture_Documentation.md` for more details.*

## System Managers

Game logic for specific systems (Shops, Crafting, Salvaging) is decoupled into dedicated Manager ScriptableObjects, manipulating the state of the Inventory and Player Data via the Event Architecture.

**Dependency Chain (e.g., Shop):**
**ShopManagerSO** -> **GameSessionSO** -> **PlayerData.Gold** / **PlayerInventory**

*   **ShopManagerSO** reads/modifies the global state via `GameSessionSO` and temporarily caches the NPC's `InventoryManager`.
*   **CraftingManagerSO** and **SalvageManagerSO** read from `CraftingRegistrySO` to validate recipes and modify the player's state via `GameSessionSO`.

## UI Interactions and Drag-and-Drop

Interactions within the UI are abstracted into semantic events to decouple views from game logic.

**Dependency Chain (Interaction):**
**PointerEvent** (UI Toolkit) -> **InventorySlotView** -> **UIInventoryEventsSO** -> **Context Override Views** / **Managers**

*   Raw **PointerEvents** (`PointerDown`, `PointerMove`, `PointerUp`) are captured by **InventorySlotView**.
*   The slot view translates these into semantic events like `OnItemRightClicked` or `OnRequestMoveItem` and fires them via **UIInventoryEventsSO**.
*   Context-specific views (like `ShopSubView`) or centralized arbitrators (like `InventoryTransferManagerSO`) listen to these events to execute specific logic.

*See `UI_Interactions_Documentation.md` for more details.*

## Equipment Management Architecture

The equipment system integrates the player's inventory directly with their stats via a dedicated controller and effect bridge.

**Dependency Chain:**
**InventoryManager** -> **PlayerEquipmentController** -> **EquipmentEffectBridge** -> **PlayerEffectSourceController** -> **PlayerEffectResolver**

*   **InventoryManager** (MonoBehaviour) acts as the Equipment Container (e.g., 5 slots).
*   **PlayerEquipmentController** maps hardcoded inventory indices to `EquipmentSlot` enums and invokes `OnEquippedItemChanged`.
*   **EquipmentEffectBridge** extracts stats (`StrengthBonus`, etc.) from the item's `EquipableComponent` and injects them into the **PlayerEffectSourceController**.
*   **PlayerEffectResolver** applies the numeric modifiers to the final stats.

*See `Equipment_System_Documentation.md` for more details.*

## Event Architecture

The project employs a robust event-driven architecture using ScriptableObjects to decouple systems. Systems listen to these events rather than holding hard references to each other.

* **UIEventsSO**
  * **UIManager** -[listens to]-> `OnRequestOpen`, `OnRequestClose`, `OnRequestCloseAll`
  * **ShopManagerSO** -[listens to]-> `OnRequestOpen` (To cache dynamic shop containers)
  * **Views / Triggers** -[invokes]-> `OnRequestOpen` / `OnRequestClose`
* **UIInventoryEventsSO**
  * **InventoryManager** -[invokes]-> `OnInventoryUpdated`
  * **ShopManagerSO** -[listens to]-> `OnRequestBuy`, `OnRequestSell`
  * **CraftingManagerSO** -[listens to]-> `OnRequestForge`
  * **SalvageManagerSO** -[listens to]-> `OnRequestSalvage`
  * **Views (e.g., `ShopSubView`, `ForgeSubView`)** -[invokes]-> Transaction Requests (`OnRequestBuy`, `OnRequestForge`, etc.)
  * **Managers** -[invokes]-> `OnCurrencyChanged`, `OnShopInventoryUpdated`, `OnInventoryUpdated` (post-transaction)
