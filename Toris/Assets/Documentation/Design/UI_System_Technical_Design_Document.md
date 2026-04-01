# Outland Haven: UI System Technical Design Document

## 1. Core Architectural Pillars
To ensure scalability and prevent tight coupling, all UI development must adhere to these three pillars:

* **Strict MVP Pattern:** UI Views are completely "dumb." They never hold game logic, modify stats, or process transactions. Logic resides entirely in Presenters/Controllers and dedicated System Managers.
* **Event-Driven Communication:** Systems communicate via `ScriptableObject` events (e.g., `UIEventsSO`, `UIInventoryEventsSO`). A UI button click does not call a manager directly; it fires an event that the manager listens for.
* **The Architecture of Restraint:** The UI enforces gameplay. The inventory is intentionally constrained to force resource attrition and "hoarder's dilemmas." Upgrades prioritize tag-based, in-inventory synergies over bloated crafting menus.

---

## 2. Data & State Synchronization
The UI must accurately reflect the game state without ever owning it. We use a **Hybrid Architecture** to achieve this.

### State Management
* **Mutable State Avoidance:** We do not use `PlayerDataSO` for volatile stats (Health, Gold, Stamina, Level).
* **Runtime Anchors:** Global systems (like `ShopManagerSO`) access the player's core `MonoBehaviours` via injected ScriptableObject anchors (e.g., `PlayerProgressionAnchorSO`).
* **The UI Bridge:** The UI never uses Anchors. Instead, a `PlayerHUDBridge` MonoBehaviour sits on the player, listens to internal state changes, and re-emits them as generic C# events (`OnHealthChanged`, `OnStaminaChanged`). The UI only subscribes to this Bridge.

---

## 3. The Base UI Framework
All UI windows operate under a unified lifecycle managed by the `UIManager`.

### The UIManager
The central authority attached to the root `UIDocument`.
* **ScreenZones:** Defines layout areas via `ScreenZone` (HUD, Left, Right, Modal).
* **Mutual Exclusivity:** Opening a new screen in `ScreenZone.Left` automatically closes the previous one in that zone.

### Controllers & Lifecycles
| Phase | Responsibility |
| :--- | :--- |
| **Awake()** | Cache dependencies and find the `UIManager`. |
| **OnEnable()** | Validate required UXML templates and subscribe to global events. |
| **Start()** | Instantiate the `VisualTreeAsset`, construct the `GameView`, pass dependencies, and register the view to a specific `ScreenZone`. |

> [!TIP]
> **SubView Pattern:** Complex screens (like NPCs) use a parent `GameView` that lazily instantiates nested SubViews (e.g., `ShopSubView`) upon tab selection, destroying or hiding them when switching tabs.

---

## 4. Item & Inventory Logic
The inventory system uses a highly decoupled data structure.

* **Flyweight Blueprints:** `InventoryItemSO` holds static data (Icon, Name, Max Stack, Base Value, static Tags).
* **Dynamic Instances:** `ItemInstance` holds the `InventoryItemSO` reference and a list of dynamically generated `ItemComponentState` objects (e.g., Durability, Level) to track runtime mutations. 
* **The Container:** `InventoryManager` is the universal container script. It handles slot capacity, stack splitting, and adding/removing items. Used for Player Backpack, Equipment, and NPC Shops.

---

## 5. Specific UI Module Implementations

### A. The HUD (Heads-Up Display)
* **Zone:** `ScreenZone.HUD` (Persistent overlay).
* **Data Source:** Binds exclusively to the `PlayerHUDBridge` for Health, Stamina, Level, and Gold.
* **Quick Slots:** Visualizes bound abilities. Key presses trigger `UIInventoryEventsSO.OnRequestConsume`, bypassing UI interaction for immediate effect.

### B. Player Inventory & Equipment
* **Zone:** `ScreenZone.Right`.
* **Equipment Logic:** Dragging an item into a slot triggers the `EquipmentEffectBridge`, injecting stats (e.g., `StrengthBonus`) into the `PlayerResolvedEffects` struct.
* **Frictionless Consumption:** Right-clicking a consumable fires an event. `ConsumableManagerSO` intercepts this, scans adjacent items for synergistic `[ItemTag]` components, calculates multipliers, and applies the effect.

### C. The Smith (Functional NPC)
* **Zone:** `ScreenZone.Left` (Triggers Player Inventory to open in `ScreenZone.Right`).
* **Tabs:**
    * **Shop:** Displays NPC's `InventoryManager`. Right-clicking fires `OnRequestBuy/Sell`.
    * **Salvage:** A drop-zone utilizing proxy slots. Processing fires `OnRequestSalvage` to convert gear into ores.
    * **Forge:** Uses `CraftingRecipeSO` combined with salvaged ores for permanent upgrades.

### D. Milestone Screen (Level-Up)
* **Zone:** `ScreenZone.Modal` (Full-screen interrupt).
* **Behavior:** Pauses gameplay when `PlayerProgression` hits a threshold. Displays tooltips for new `AbilitySO` assets. Requires explicit acknowledgment to resume.
