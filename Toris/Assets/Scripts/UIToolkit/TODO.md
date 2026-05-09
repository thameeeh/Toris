# Project Roadmap: UI & Save Integration

This document outlines our high-level goals and progress for connecting the Main Menu to the core gameplay experience.

---

## 🤖 Session Hand-off Message
**Status:** Potion HUD and Hotkey integration task is fully complete. The Save/Load system and Pause menus are stable.

**Key Achievements Today:**
- **Potion HUD Integration:** Added a central potion quickbar to the HUD with two slots. The HUD dynamically tracks the Potion Inventory, allowing for visual feedback and immediate right-click usage.
- **Hotkey Usage:** Mapped keyboard inputs ('1' and '2') to trigger item consumption directly from the Potion quickbar slots, properly handling charges and item depletion.
- **Standardization:** Universal right-click consumption confirmed across all item containers.

**Action Required (Unity Inspector Final Setup):**
1. **HudScreenController:** Assign `_slotTemplate` and `_potionInventory`.
2. **InventoryActionController:** Assign `_potionInventory` and `_inputReader`.

**Next Steps:**
- Monitor the **Known Issues** below regarding the inventory drag-and-drop bug when you return to inventory systems.
- The UI architecture is now highly modular and ready for further screen implementations using the established MVP patterns.

---

## 🚩 Known Issues & Architectural Debt
- **Inventory Drag-and-Drop Bug:** Dynamically instantiated slots sometimes lose pointer interaction or fail to register drops due to `TemplateContainer` wrapper properties. (Requires targeted fix in `InventorySlotView.cs` and layout analysis).
- **Logic Fragmentation (Consumables):** Consumption logic is split between `PlayerConsumableController` and `ConsumableManagerSO`. Future logic (sounds, cast times) must be duplicated or consolidated into an `IUsable` component pattern.
- **O(N) Component Lookups:** Frequent calls to `GetComponent<T>()` on `ItemInstance` use linear searches through `[SerializeReference]` lists. This may cause performance micro-stuttering in late-game scenarios with high item counts.
- **Closed-for-Extension Controllers:** `InventoryActionController` uses hardcoded type checks for `ConsumableComponent` and `EquipableComponent`. This should be refactored to a Command or Interface-based approach (`IInteractable`) to support new item behaviors without modifying the controller.
- **Passive UI Updates:** UI relies on manual pokes from controllers rather than the Model (`ItemInstance`) broadcasting its own state changes. Durability or background timer updates will be difficult to synchronize.

---

## Main Objective: Item System Scalability & UX Polish
Ensure the inventory and item systems are robust, easily extensible for new content, and provide excellent player feedback.

## Core Milestones (Next Phase)

- [X] **Phase 1: Item Architecture Refactor**
  - [X] **Step 1: Define Behavior Interfaces:** Create `IUsableComponent` and `IEquipableComponent` to define capabilities rather than types.
  - [X] **Step 2: Empower the Components:** Move usage/equip logic (e.g., HP restoration) out of centralized controllers and directly into the components.
  - [X] **Step 3: Cache Interfaces for Performance:** Update `ItemInstance` or `InventoryItemSO` to perform a one-time scan of components, providing O(1) access to `UsableBehavior` and `EquipableBehavior`.
  - [X] **Step 4: Refactor InventoryActionController:** Replace hardcoded type checks with generic interface calls, making the system "Open for Extension" for new item types like throwables or readable books.
  - [X] Consolidate consumable logic to resolve fragmentation between controllers and managers.
- [ ] **Phase 2: Drag-and-Drop Mastery**
  - [ ] Implement visual slot highlighting (e.g., green for valid, red for invalid) during drag operations based on `SlotFilterType`.
  - [X] Resolve any remaining pointer interception bugs on dynamic slots. (Fixed in Phase 1 via `pickingMode = Ignore`).
- [ ] **Phase 3: The Generic Action Bar**
  - [ ] Abstract the Potion HUD into a generic Action Bar.
  - [ ] Allow assigning both Consumables and Active Skills to the hotkeys.

---

## Main Objective: Seamless Game Loading (STABLE)
Ensure that selecting a save slot from the Main Menu instantly restores the player's progress and transports them exactly where they left off in the game world.

## Core Milestones

- [X] **System Wiring** (COMPLETED)
- [X] **Data Restoration** (COMPLETED)
- [X] **World Transition** (COMPLETED)
- [X] **Fresh Starts** (COMPLETED)
- [X] **Dynamic Menu Information** (COMPLETED)
- [X] **Automatic Restoration (No F9 required)** (COMPLETED)
- [X] **Polymorphic State Serialization** (COMPLETED)
- [X] **Save Deletion** (COMPLETED)
- [X] **Confirmation Modal (Save Deletion & Exit)** (COMPLETED)
- [X] **Pause Menu Implementation** (COMPLETED)
- [X] **UI Polish & Consistency** (COMPLETED)
- [X] **Equippable Stacking Refactor** (COMPLETED) - Enforced MaxStackSize=1 for equippables and implemented swap-on-drag behavior.
- [X] **Hotkey-Driven Potion Consumption** (COMPLETED) - Added potion HUD quickbar, hotkey inputs ('1' and '2'), and unified consumable usage.
- [X] **Scene Transition Reliability** (COMPLETED) - Fixed equipment data loss and implemented robust container identification (`IsEquipment` flag).


**Phase 1: Item Architecture Refactor**

The problem:
   1. InventoryActionController has hardcoded knowledge of ConsumableComponent and EquipableComponent.
      If we add ReadableComponent (for lore books) or ThrowableComponent (for bombs), we have to open
      InventoryActionController and add new TryRead() / TryThrow() methods. This violates the
      Open/Closed Principle.
   2. slot.HeldItem.BaseItem.GetComponent<T>() traverses a list every time the UI needs to know if a
      button should be enabled/disabled.

  The Solution (Phase 1 Refactoring):
  Introduce interfaces for the capabilities of an item, and interfaces for the components themselves.
   - Create IUsableItem interface.
   - Create IEquipableItem interface.

  Instead of InventoryActionController asking "Are you a Consumable?", it should ask "Do you implement
  IUsableItem?".
  if (slot.HeldItem.BaseItem is IUsableItem usable)
  usable.Use(playerStats);

  Wait, the components are held inside the InventoryItemSO's Components list. So the ItemComponent
  itself would implement IUsableItem.

   1 public interface IUsableItemComponent
   2 {
   3     bool CanUse(GameObject user, ItemInstance itemState);
   4     void Use(GameObject user, ItemInstance itemState);
   5 }

  Then InventoryActionController doesn't need to know what the item is, just that it has a component
  implementing IUsableItemComponent.

  Wait, how do we avoid the O(N) lookup?
  If InventoryItemSO is a ScriptableObject, it's evaluated at edit time or load time.
  We can cache interfaces on the InventoryItemSO or the ItemInstance.
  For example, in ItemInstance or InventoryItemSO.OnEnable(), we can do a one-time pass:
  public IUsableItemComponent UsableComponent { get; private set; }

  Let's write out the current state, the proposed change, and the impact.

  Current State:
   - InventoryActionController acts as a middleman with hardcoded if (component is Consumable)
     statements.
   - ConsumableComponent holds data, but the logic is executed by PlayerConsumableController.
   - Looking up if an item is a consumable requires an O(N) search through the Components list of the
     InventoryItemSO blueprint.

  Proposed Change:
   1. Define Interfaces: IUsable and IEquipable.
   2. Move logic into Components: The ConsumableComponent implements IUsable and contains the logic of
      what happens when consumed (restoring health/mana), taking necessary dependencies via an
      injection context.
   3. Optimize Lookups: Instead of iterating the component list every time, ItemInstance or
      InventoryItemSO caches these interfaces upon creation (e.g., public IUsable UsableBehavior {
      get; private set; }).
   4. Dumb Controllers: InventoryActionController shrinks. HandleRequestUse becomes if
      (item.UsableBehavior != null) item.UsableBehavior.Use(playerContext);.

  Impact on Architecture:
   - Open/Closed Principle: Adding a new item type (e.g., ThrowableBombComponent : ItemComponent,
     IUsable) requires zero changes to InventoryActionController. The controller just calls .Use().
   - Performance: Caching the interface prevents micro-stuttering from list traversal during UI
     updates.
   - Encapsulation: The logic for what a consumable does is co-located with its data
     (ConsumableComponent), rather than split into a separate PlayerConsumableController.

  Let me formulate the response based on this analysis.Here is an analysis of the current state of the
  codebase regarding Phase 1: Item Architecture Refactor, my proposed changes, and how it will affect
  future development.

  1. Current State of the Codebase

  Currently, the interaction logic is tightly coupled and centralized in the Controllers, violating
  the Open/Closed Principle.

   * The Controller Bottleneck: InventoryActionController.cs acts as a God Object for item
     interactions. It contains hardcoded type checks (GetComponent<ConsumableComponent>() and
     GetComponent<EquipableComponent>()).
   * Logic Fragmentation: The data for a consumable lives in ConsumableComponent, but the logic for
     what happens when you consume it lives inside PlayerConsumableController.cs.
   * O(N) Lookups: To determine if an item can be equipped or used (which the UI asks constantly to
     enable/disable buttons), the system calls slot.HeldItem.BaseItem.GetComponent<T>(). This method
     performs a linear foreach loop over the ItemComponent list on the ScriptableObject. While small
     now, this will cause micro-stutters late-game when the player is rapidly moving hundreds of
     items.

  2. My Suggestion: The Interface & Caching Pattern

  We need to invert the dependency. Controllers should not ask what an item is; they should only ask
  what an item can do.

  Step 1: Define Behavior Interfaces
  We will create interfaces like IUsableComponent and IEquipableComponent.

   1 public interface IUsableComponent
   2 {
   3     bool CanUse(GameObject user, ItemInstance itemState);
   4     void Use(GameObject user, ItemInstance itemState);
   5 }

  Step 2: Empower the Components
  We modify ConsumableComponent to implement IUsableComponent. The logic for restoring health/mana
  moves out of the PlayerConsumableController and directly into the ConsumableComponent.Use() method.

  Step 3: Cache the Interfaces (Eliminating O(N) lookups)
  We modify ItemInstance (or InventoryItemSO) to perform a one-time scan of its components upon
  creation.

   1 // Inside ItemInstance.cs
   2 public IUsableComponent UsableBehavior { get; private set; }
   3
   4 public ItemInstance(InventoryItemSO baseItem) {
   5     // ... setup states ...
   6     // One-time O(N) lookup cached for O(1) access later
   7     UsableBehavior = baseItem.GetComponent<IUsableComponent>();
   8 }

  Step 4: "Dumb Down" the Controller
  InventoryActionController loses hundreds of lines of code. It now simply says:

   1 private void HandleRequestUse(InventorySlot slot) {
   2     if (slot.HeldItem.UsableBehavior != null) {
   3         slot.HeldItem.UsableBehavior.Use(PlayerGameObject, slot.HeldItem);
   4     }
   5 }

  3. How this affects current and future architecture

   * Massive Scalability: If you want to add a ThrowableBombComponent tomorrow, you just make it
     implement IUsableComponent. You do not have to touch the UI, the InventoryActionController, or
     any other core system. It "just works" out of the box.
   * Performance: UI updates become extremely fast. Determining if the "Use" button should be active
     drops from an O(N) list traversal to an O(1) null check (if (item.UsableBehavior != null)).
   * Cleaner Architecture: The MVP pattern is strengthened. The Model (Item Components) dictates the
     logic, and the Presenter (InventoryActionController) merely routes the player's button press to
     the item.