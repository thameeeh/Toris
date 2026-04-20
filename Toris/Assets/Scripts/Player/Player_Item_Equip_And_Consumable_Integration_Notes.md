# Player Item Equip And Consumable Integration Plan

This document is the player-side implementation plan we should follow later.

It is intentionally a planning document only. It does not mean the runtime is already integrated.

## Source Of Truth

### Equipment Documentation Status

The project equipment documentation is useful, but only partially current.

Safe to trust:

* equipment is backed by a dedicated `InventoryManager`
* `PlayerEquipmentController` maps fixed indices to `EquipmentSlot`
* `EquipmentEffectBridge` pushes equipped item effects into the player effect system
* gameplay reads equipped items through `PlayerEquipmentController`

Do not trust as the active interaction source:

* the part saying click-to-equip and unequip is currently managed by `PlayerEquipmentController`

The live active equip path is drag-and-drop:

1. `InventorySlotView` emits `OnRequestMoveItem`
2. `InventoryTransferManagerSO` validates and moves/swaps items
3. `PlayerEquipmentController` reacts after inventory refresh

Relevant references:

* [Equipment_System_Documentation.md](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Documentation/Equipment_System_Documentation.md)
* [InventorySlotView.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/UIToolkit/Template%20controlls/InventorySlotView.cs)
* [InventoryTransferManagerSO.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Player/Player/Inventory/InventoryTransferManagerSO.cs)
* [PlayerEquipmentController.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Player/Player/Equipment/PlayerEquipmentController.cs)

## Locked Decisions

These are now treated as chosen design decisions for implementation:

* `Mana` in consumable data means `stamina`
* consumables are click-to-consume items
* consuming can create:
  * an instant effect, like healing or stamina restoration
  * a timed effect, like a temporary buff
* consumables should not be folded into the current 5-slot equipment mapping

## Current State

### Equipment

Equipment is already integrated.

The current 5-slot mapping is hardcoded in multiple places:

* `0 = Head`
* `1 = Chest`
* `2 = Legs`
* `3 = Arms`
* `4 = Weapon`

Relevant files:

* [PlayerEquipmentController.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Player/Player/Equipment/PlayerEquipmentController.cs)
* [PlayerEquipmentView.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/UIToolkit/UI/UIViews/PlayerEquipmentView.cs)
* [InventoryTransferManagerSO.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Player/Player/Inventory/InventoryTransferManagerSO.cs)

### Consumables

Consumable data already exists:

* `ConsumableComponent`
* `ConsumableState`
* payload
* amount
* cooldown
* max charges

But runtime use does not exist yet.

Right now `InventoryActionController.HandleRequestUse(...)` stops at a log.

Relevant files:

* [ConsumableModule.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Items/Entity%20Modules/ConsumableModule.cs)
* [InventoryActionController.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Player/Player/Inventory/InventoryActionController.cs)

### Player Runtime Targets Already Exist

Instant resource consumables already have good runtime targets:

* `HP -> PlayerStats.RestoreHealth(...)`
* `Mana -> PlayerStats.RestoreStamina(...)`

The clean dependency path is:

* [PlayerStatsAnchorSO.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Player/Player/Anchors/PlayerStatsAnchorSO.cs)

Relevant runtime:

* [PlayerStats.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Player/Player/Status/PlayerStats.cs)
* [PlayerEffectSourceController.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Player/Player/Status/PlayerEffectSourceController.cs)

## Important Constraints

### Constraint 1: The Existing Equipment Container Should Stay A 5-Slot Equipment Container

Do not extend the current equipment inventory to absorb consumables as slot `5` and `6`.

That would require touching multiple hardcoded equipment assumptions and is not the right first integration.

### Constraint 2: Generic Slot Click Events Are Too Ambiguous

This is a major implementation detail.

`InventorySlotView` currently emits:

* `OnItemClicked(InventorySlot slot)`
* `OnItemRightClicked(InventorySlot slot)`

That event payload does not include the owning container.

So if both:

* player inventory slots
* equipment slots

emit the same click event, the consumer cannot reliably know where the click came from.

That means we should not build consumable use by blindly subscribing to the generic click event and guessing context.

Relevant files:

* [InventorySlotView.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/UIToolkit/Template%20controlls/InventorySlotView.cs)
* [UIInventoryEventsSO.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/UIToolkit/UI/Events/UIInventoryEventsSO.cs)

### Constraint 3: Charged Consumables And Stacks Need A Rule

Current inventory data works like this:

* an `InventorySlot` stores one `HeldItem` and one `Count`
* `ItemInstance` stores per-instance runtime state
* `ConsumableState` stores `CurrentCharges`

So if one item in a stack loses a charge, that stack now wants mixed per-instance state.

That does not fit cleanly into the current slot model.

### Constraint 4: Cooldown Exists In Data, But Not Yet In Runtime

`ConsumableComponent.CooldownDuration` already exists.

There is no live player-side cooldown owner for consumables yet.

## Recommended Architecture

### 1. Keep `InventoryActionController` As The Action Gateway

Do not turn `PlayerInventoryView` into a logic-heavy consumer.

Keep the current event-driven structure:

* UI emits a request
* controller validates
* runtime system mutates data
* UI rebuilds from authoritative inventory state

`InventoryActionController` should stay the entry point for:

* equip request
* unequip request
* use request

But it should not grow into a god object.

### 2. Add A Dedicated `PlayerConsumableController`

Recommended new runtime owner:

* `PlayerConsumableController`

Reason:

* keeps `InventoryActionController` small
* gives cooldowns, charge consumption, and effect application a clear home
* scales better once timed consumables are added

Recommended responsibility split:

* `InventoryActionController`
  * receives `OnRequestUse`
  * validates that the clicked slot holds a consumable
  * forwards to consumable runtime
* `PlayerConsumableController`
  * applies effects
  * manages charges
  * manages cooldowns
  * handles stack cleanup rules
  * fires inventory refresh when authoritative mutation is complete

### 3. Add A Player-Inventory-Specific Click Path

Because the generic click event is ambiguous, the clean approach is:

* keep generic `InventorySlotView` behavior for drag/drop and shared screens
* add a player-inventory-specific click request path for the player grid

Recommended direction:

* player inventory grid emits a dedicated event that includes container context
* equipment slots keep their current drag-and-drop path

Good options:

1. add a new event in `UIInventoryEventsSO` for player inventory slot interaction
2. create a player-grid-specific slot view wrapper that emits the dedicated player inventory request event

Recommended event shape:

* container + slot

not just:

* slot

This preserves MVP and avoids hidden context guesses.

### 4. Treat Timed Consumables As Effect Sources, Not Direct Stat Writes

Instant consumables should mutate `PlayerStats`.

Timed consumables should not directly patch stats and hope cleanup happens later.

Instead, timed consumables should eventually use the player effect system through:

* [PlayerEffectSourceController.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Player/Player/Status/PlayerEffectSourceController.cs)

That keeps temporary buffs aligned with how passive equipment effects are already resolved.

## V1 Scope

The first real implementation pass should be intentionally narrow.

### V1 Includes

* click-to-consume from the main player inventory grid
* immediate HP restore
* immediate stamina restore
* charge consumption
* single item removal when depleted
* inventory refresh after successful use

### V1 Explicitly Does Not Include

* extending the 5-slot equipment container
* binding the decorative consumable placeholder in `PlayerInventory.uxml`
* quick-use potion slots
* timed buff consumables
* dynamic stack splitting for charged items

## V1 Runtime Rules

### Rule 1: `Mana` Maps To Stamina

Runtime mapping:

* `HP -> RestoreHealth`
* `Mana -> RestoreStamina`

### Rule 2: Multi-Charge Consumables Must Not Stack

This should be the first implementation rule unless we intentionally build a stack-splitting system later.

Recommended rule:

* if `MaxCharges > 1`, require `MaxStackSize = 1`

That is the safest fit for the current inventory model.

### Rule 3: Clicking A Consumable Uses It Immediately

There is no equip step.

The flow should be:

1. player clicks a player-grid slot
2. click router identifies slot context
3. if item is consumable, emit use request
4. action controller validates request
5. consumable controller consumes item
6. inventory refresh event rebuilds UI

### Rule 4: Clicking An Equipable Item Can Still Route To Equip

The same player-grid interaction layer can later support:

* equipable item -> request equip
* consumable item -> request use

But the routing decision belongs in the controller layer, not in the dumb view.

## Implementation Sequence

### Phase 1: Event Contract Cleanup

Goal:

* make player inventory clicks identifiable without breaking forge, salvage, or shop flows

Plan:

1. add a dedicated player inventory interaction request event to `UIInventoryEventsSO`
2. make the player inventory grid emit that event
3. leave equipment slots and shared slot views on their current drag/drop path

Files to touch later:

* [UIInventoryEventsSO.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/UIToolkit/UI/Events/UIInventoryEventsSO.cs)
* [PlayerInventoryView.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/UIToolkit/UI/UIViews/PlayerInventoryView.cs)
* [InventorySlotView.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/UIToolkit/Template%20controlls/InventorySlotView.cs)

### Phase 2: Runtime Consumable Owner

Goal:

* create the authoritative consume path

Plan:

1. add `PlayerConsumableController`
2. give it access to:
   * `PlayerStatsAnchorSO`
   * player inventory
   * UI inventory events
3. support instant HP/stamina consumables first
4. enforce the stack rule for charged items
5. refresh inventory only after successful mutation

Files to touch later:

* new `PlayerConsumableController.cs`
* [InventoryActionController.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Player/Player/Inventory/InventoryActionController.cs)
* [PlayerStatsAnchorSO.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Player/Player/Anchors/PlayerStatsAnchorSO.cs)
* [PlayerStats.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Player/Player/Status/PlayerStats.cs)

### Phase 3: Timed Consumables

Goal:

* support buff-style potions cleanly

Plan:

1. extend consumable runtime to create a temporary effect source
2. register it with `PlayerEffectSourceController`
3. remove it when duration ends

This should be a separate pass after V1 works.

### Phase 4: Optional Dedicated Consumable Slots

The `Consumables__Player--Inventory` area in `PlayerInventory.uxml` should be treated as optional future work.

If we later want quick-use potion slots, that should be a separate equipped-consumable system, not mixed into the first click-to-consume pass.

## File Ownership Guide For The Future Implementation

### Safe To Treat As Sources Of Truth

* [PlayerEquipmentController.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Player/Player/Equipment/PlayerEquipmentController.cs)
* [InventoryTransferManagerSO.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Player/Player/Inventory/InventoryTransferManagerSO.cs)
* [PlayerStats.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Player/Player/Status/PlayerStats.cs)
* [ConsumableModule.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Items/Entity%20Modules/ConsumableModule.cs)

### Use Carefully

* [Equipment_System_Documentation.md](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Documentation/Equipment_System_Documentation.md)

Reason:

* good passive architecture overview
* stale on the active click-to-equip description

### Do Not Use As The First Integration Target

* the consumable placeholder slots inside [PlayerInventory.uxml](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/UI_Toolkit/UXMLs/PlayerInventory.uxml)

Reason:

* they are currently just placeholder visuals
* they are not required for the first click-to-consume implementation

## Practical Recommendation

When we start coding this, the first pass should be:

1. create a player-inventory-specific click request path
2. add `PlayerConsumableController`
3. wire `InventoryActionController` to forward valid use requests
4. support only instant HP and stamina consumables
5. enforce `MaxCharges > 1 => MaxStackSize = 1`
6. leave timed buffs and quick slots for later

That keeps the current equipment system stable, respects the existing MVP and event-driven architecture, and gives consumables a clean dedicated runtime path instead of forcing them into the wrong system.
