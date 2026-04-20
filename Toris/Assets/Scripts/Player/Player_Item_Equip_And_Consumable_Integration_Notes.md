# Player Item Equip And Consumable Integration Guide

Use this document as the working reference for player equipment and future consumable integration.

This guide is meant to describe how the system should be understood and where future work should go.

## Current Backend State

Treat the first consumable backend pass as complete.

The current player-owned runtime now supports:

* right-click consume from the main inventory in `Normal` context
* instant health consumables
* instant stamina consumables
* timed buff consumables through the player effect pipeline
* health regeneration over time through `HealthRegenPerSecond`
* per-item cooldown handling
* charge consumption and depletion cleanup

Treat that as the stable baseline.

Do not reopen this backend unless a new gameplay requirement appears.

## Purpose

Separate passive equipment behavior from active consumable behavior.

Use these principles:

* keep equipment as a persistent stat and gameplay source
* keep consumables as active use items
* let UI emit requests and let runtime controllers mutate authoritative state
* avoid bending the 5-slot equipment layout into a consumable system

## Equipment Model

Treat the equipment system as a dedicated 5-slot inventory-backed system.

Current slot meaning:

* `0 = Head`
* `1 = Chest`
* `2 = Legs`
* `3 = Arms`
* `4 = Weapon`

Use these files as the main runtime references:

* [PlayerEquipmentController.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Player/Player/Equipment/PlayerEquipmentController.cs)
* [PlayerEquipmentView.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/UIToolkit/UI/UIViews/PlayerEquipmentView.cs)
* [InventoryTransferManagerSO.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Player/Player/Inventory/InventoryTransferManagerSO.cs)

## Equipment Interaction

Use two active interaction paths for equipment:

### Drag And Drop

Use `InventorySlotView` local move events and let `InventoryTransferManagerSO` perform the authoritative slot mutation.

The drag path currently supports:

* cross-container moves
* swaps
* partial stack movement through `amountToMove`
* targeted refreshes through `OnSpecificSlotsUpdated`

### Click And Right Click

Use `PlayerInventoryView` to route main inventory right clicks by context:

* `Normal` -> equip request
* `Shop` -> sell request
* `Salvage` -> salvage request

Use `PlayerEquipmentView` to route equipment slot click and right-click to:

* unequip request

That means click-driven equip and unequip are active again.

Use these files as the interaction reference:

* [InventorySlotView.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/UIToolkit/Template%20controlls/InventorySlotView.cs)
* [PlayerInventoryView.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/UIToolkit/UI/UIViews/PlayerInventoryView.cs)
* [PlayerEquipmentView.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/UIToolkit/UI/UIViews/PlayerEquipmentView.cs)
* [UIInventoryEventsSO.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/UIToolkit/UI/Events/UIInventoryEventsSO.cs)
* [InventoryInteractionContext.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/UIToolkit/UI/Events/InventoryInteractionContext.cs)

## Consumable Model

Treat consumables as click-to-consume items.

Use these locked meanings:

* `HP` means health restoration or health-related effect
* `Mana` means stamina restoration or stamina-related effect

Consumables may support:

* instant effects, such as healing or stamina restore
* timed effects, such as temporary buffs or regeneration-over-time

## Consumable Runtime

The item data already exists:

* `ConsumableComponent`
* `ConsumableState`
* payload
* amount
* cooldown
* max charges

The first runtime use path is now active.

Right now:

* `OnRequestUse` exists on the event channel
* `PlayerInventoryView` emits `OnRequestUse` for consumables when the player right-clicks in `Normal` context
* `InventoryActionController` receives the request and forwards it into `PlayerConsumableController`
* `PlayerConsumableController` applies instant HP and stamina consumables
* `PlayerConsumableController` applies timed buff consumables through `PlayerEffectSourceController`
* consumable cooldown is tracked per item definition
* timed consumables refresh their own active duration by consumable item type
* active timed buffs expire through runtime ticking in `InventoryActionController`
* slot updates are pushed through `OnSpecificSlotsUpdated`

Use these files as the current consumable reference:

* [ConsumableModule.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Items/Entity%20Modules/ConsumableModule.cs)
* [InventoryActionController.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Player/Player/Inventory/InventoryActionController.cs)
* [UIInventoryEventsSO.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/UIToolkit/UI/Events/UIInventoryEventsSO.cs)

## Runtime Targets

Use `PlayerStats` for instant resource consumables:

* `HP -> PlayerStats.RestoreHealth(...)`
* `Mana -> PlayerStats.RestoreStamina(...)`

Use `PlayerStatsAnchorSO` as the clean dependency anchor:

* [PlayerStatsAnchorSO.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Player/Player/Anchors/PlayerStatsAnchorSO.cs)

Use `PlayerEffectSourceController` for timed buff consumables:

* [PlayerEffectSourceController.cs](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Scripts/Player/Player/Status/PlayerEffectSourceController.cs)

Use these consumable authoring fields for timed buffs:

* `EffectMode = TimedPlayerEffect`
* `TimedEffectDefinition = PlayerEffectDefinitionSO asset to apply`
* `TimedEffectDuration = active buff duration in seconds`

Use `HealthRegenPerSecond` in `PlayerEffectDefinitionSO` when a timed consumable should heal over time.

## Structural Rules

### Keep Equipment And Consumables Separate

Do not extend the current equipment inventory to absorb consumables.

The current equipment layout is too specific and too hardcoded for that to be a good first integration.

### Keep `InventoryActionController` As The Action Gateway

Use `InventoryActionController` to receive:

* equip request
* unequip request
* use request

Keep views dumb.
Let views emit requests.
Let controllers validate and mutate state.

### Use `PlayerConsumableController` As The Runtime Owner

Use the dedicated runtime owner for consumables.

Recommended responsibilities:

* apply instant effects
* apply timed buff effects
* manage charges
* manage cooldowns
* manage active timed source expiry
* handle stack cleanup
* emit inventory refresh after authoritative mutation

Keep `InventoryActionController` small by forwarding use requests into that controller.

## Main Inventory Routing Rule

Use the existing `Normal` inventory context as the player inventory consume entry point.

Use this main inventory right-click behavior in `Normal` context:

1. if item is consumable -> emit `OnRequestUse`
2. else if item is equipable -> emit `OnRequestEquip`
3. else do nothing

That fits the current interaction architecture better than inventing a new parallel click system.

## Stack And Charge Rule

Treat stacked charged consumables as a special case that needs a clear restriction.

Current data model:

* `InventorySlot` stores one `HeldItem` and one `Count`
* `ItemInstance` stores runtime state
* `ConsumableState` stores `CurrentCharges`

Because of that, mixed-charge stacks do not fit cleanly.

Use this first implementation rule:

* if `MaxCharges > 1`, require `MaxStackSize = 1`

That is the safest fit for the current inventory model.

## Active V1 Scope

Keep the current consumable pass narrow.

### Include

* right-click consume from the main player inventory in `Normal` context
* instant HP restore
* instant stamina restore
* timed buff consumables through `PlayerEffectSourceController`
* health-over-time consumables through `HealthRegenPerSecond`
* charge consumption
* item removal when depleted
* inventory refresh after successful use

### Exclude

* extending the equipment inventory
* quick-use potion slots
* binding the decorative consumable placeholder in `PlayerInventory.uxml`
* active buff visuals
* cooldown visuals
* live stats panel integration
* dynamic stack splitting for charged items

## Next Implementation Order

When the next consumable expansion begins, follow this order:

1. keep the current right-click use route in `PlayerInventoryView`
2. keep `OnRequestEquip` for equipables
3. keep `PlayerConsumableController` as the runtime owner
4. keep timed buffs in `PlayerEffectSourceController`
5. add quick-use consumable slots only when input and ownership are agreed
6. add buff and cooldown visuals only when the UI owner is ready

## Next Work Boundary

The next consumable work is no longer backend-first.

Use this rule:

* player runtime work is complete for the first pass
* future work is mostly input, UI, or presentation driven

That means the next likely expansions are:

* quick-use consumable slots
* buff feedback
* cooldown feedback
* visual display for assigned consumables

## Documentation Usage

Use [Equipment_System_Documentation.md](C:/Users/karol/Desktop/Unity/Project%20Toris/Toris/Assets/Documentation/Equipment_System_Documentation.md) for the passive equipment overview.

Use current runtime and UI files for the active interaction flow, because that flow now spans:

* `InventorySlotView`
* `PlayerInventoryView`
* `PlayerEquipmentView`
* `InventoryActionController`
* `InventoryTransferManagerSO`

## Working Standard

When touching player items later, work from these assumptions:

* equipment stays passive and persistent
* consumables stay active and immediate
* the first consumable pass should extend the existing main inventory interaction flow
* timed effects belong in the player effect system, not in ad hoc stat patches
* new consumable work after this point should only start when a clear input or UI requirement exists
