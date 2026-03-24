# Script Dependency Documentation

This document outlines the broad architectural dependencies between the UI, Inventory, and Item systems within Outland Haven. It highlights the primary data flow, component ownership, and event-driven communication pathways.

## Core UI Architecture

The UI utilizes a Model-View-Presenter (MVP) inspired pattern, coordinated by the `UIManager`.

* **UIManager** -[manages]-> **GameView**
  * `UIManager` manages the lifecycle and screen zones (HUD, Left, Right) for all `GameView` instances.
* **ScreenController** (e.g., `InventoryScreenController`) -[instantiates/initializes]-> **GameView** (e.g., `PlayerInventoryView`)
  * Controllers instantiate UI Toolkit templates and initialize views.
* **ScreenController** -[registers]-> **UIManager**
  * Controllers register their initialized views with the `UIManager` for a specific `ScreenZone`.
* **ScreenController** -[injects]-> **GameSessionSO**, **UIEventsSO**, **UIInventoryEventsSO**
  * Controllers inject necessary ScriptableObject dependencies into the Views.
* **GameView** -[updates/interacts with]-> **VisualElement**
  * Views handle the actual visual updates and user interactions.

## Inventory & Item System

The inventory system relies heavily on ScriptableObjects for flyweight data and dynamic instances for runtime state.

* **InventoryManager** (MonoBehaviour) -[reads blueprint]-> **InventoryContainerSO**
  * `InventoryManager` reads the container blueprint to determine slot capacity and rules.
* **InventoryManager** -[registers to]-> **GameSessionSO**
  * The player's `InventoryManager` registers itself globally to `GameSessionSO.PlayerInventory` upon activation.
* **InventoryManager** -[manages]-> **InventorySlot**
  * Manages a list of live runtime slots.
* **InventorySlot** -[holds]-> **ItemInstance**
  * Slots hold runtime item wrappers containing quantity and state.
* **ItemInstance** -[references]-> **InventoryItemSO**
  * `ItemInstance` references the base item blueprint for immutable data (name, max stack size, icon, value).
* **ItemInstance** -[holds]-> **ItemComponentState**
  * Holds a list of dynamically generated runtime states (e.g., `DurabilityState`, `UpgradeableState`).
* **InventoryItemSO** -[defines]-> **ItemComponent**
  * Defines the modular behaviors (e.g., `EquipableComponent`, `ConsumableComponent`) which spawn states via a Factory Method.

## System Managers

Game logic for specific systems (Shops, Crafting, Salvaging) is decoupled into dedicated Manager ScriptableObjects, manipulating the state of the Inventory and Player Data via the Event Architecture.

* **ShopManagerSO** -[reads/modifies]-> **GameSessionSO** (Reads/Modifies `PlayerData.Gold` and `PlayerInventory`)
* **ShopManagerSO** -[caches]-> **InventoryManager** (Temporarily caches NPC shop inventory)
* **CraftingManagerSO** -[reads/modifies]-> **GameSessionSO**
* **CraftingManagerSO** -[reads]-> **CraftingRegistrySO** (Reads `CraftingRecipeSO`)
* **SalvageManagerSO** -[reads/modifies]-> **GameSessionSO**
* **SalvageManagerSO** -[reads]-> **CraftingRegistrySO** (Reads `SalvageRecipeSO`)
* **UpgradeSalvageManagerSO** -[reads/modifies]-> **GameSessionSO**
* **UpgradeSalvageManagerSO** -[reads]-> **CraftingRegistrySO**

## Equipment Management Architecture

The equipment system integrates the player's inventory directly with their stats via a dedicated controller and effect bridge.

* **InventoryManager** (MonoBehaviour) -[acts as]-> **Equipment Container**
  * An `InventoryManager` is configured specifically to hold equipment (e.g., 5 slots).
* **PlayerEquipmentController** -[reads]-> **InventoryManager**
  * The controller reads the equipment container, mapping hardcoded inventory indices to `EquipmentSlot` enums (0=Head, 1=Chest, 2=Legs, 3=Arms, 4=Weapon).
* **PlayerEquipmentController** -[invokes]-> `OnEquippedItemChanged`, `OnItemEquipped`, `OnItemUnequipped`
  * Emits events when the equipment slots change based on `UIInventoryEventsSO.OnInventoryUpdated`.
* **EquipmentEffectBridge** -[listens to]-> `OnEquippedItemChanged`
  * The logic layer that listens for equipment changes from the `PlayerEquipmentController`.
* **EquipmentEffectBridge** -[injects]-> **PlayerEffectSourceController**
  * Injects item stats (e.g., `StrengthBonus`, `DefenceBonus` from `EquipableComponent`) into the player's effect system via `EquippedItemEffectSource` and `PlayerEffectType`.
* **PlayerEffectResolver** -[resolves]-> **PlayerResolvedEffects**
  * Applies the injected numeric modifiers to update the player's `PlayerResolvedEffects` struct.
* **PlayerEquipmentView** -[renders]-> **VisualElement**
  * Handles UI rendering by dynamically creating slot instances via `InventorySlotView` and mapping them to the configured `InventoryManager`. It embeds 5 specific equipment slots (`slot-head`, `slot-chest`, `slot-legs`, `slot-arms`, `slot-weapon`) into `PlayerEquipment__Panel`.

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
