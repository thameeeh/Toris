# Player Item Drop System Plan

## Goal

Add a simple player item dropping flow that lets the player remove an item or stack quantity from their inventory and create a pickupable `WorldItem` in the scene.

The drop system should reuse the existing world pickup path:

- `WorldItem`
- `ItemPicker`
- `InventoryManager`
- `UIInventoryEventsSO`

It should not create a second pickup or inventory system.

## Non-Goals For First Pass

- No item persistence across scene reloads.
- No dropped item persistence across procedural world regeneration.
- No loot magnet behavior for player-dropped items.
- No quest pickup progress from picking up player-dropped items.
- No changes to UI view logic beyond dispatching a drop request.

## Player-Facing Rules

The first pass should support three drop inputs:

- Drag an item outside the inventory UI: drop the whole slot quantity.
- Press the drop hotkey while hovering an item slot: drop 1 item.
- Press shift plus the drop hotkey while hovering an item slot: open an amount chooser, then drop the chosen amount.

Allowed player sources:

- backpack slots
- potion slots
- equipment slots

Do not treat shop slots, crafting proxy slots, salvage/forge preview slots, or other external container slots as player-drop sources unless they are deliberately wired later.

## Existing Systems To Reuse

### `WorldItem`

File:

`Assets/Scripts/Items/WorldItem.cs`

Current behavior:

- Stores `InventoryItemSO`.
- Stores quantity.
- On pickup, creates a new `ItemInstance` from the item blueprint.
- Adds the item to the target `InventoryManager`.
- Destroys itself after successful pickup.

Important issue:

Player-dropped items may have runtime state. A dropped upgraded item, durable item, rolled item, or otherwise stateful item should not become a fresh blueprint item when picked back up.

So `WorldItem` needs a runtime item path for player drops.

### `ItemPicker`

File:

`Assets/Scripts/Player/Player/Inventory/ItemPicker.cs`

Current behavior:

- Scans nearby colliders.
- Finds `IContainerInteractable`.
- Calls `Interact(_myInventoryManager)` when the pickup input fires.

This should keep working for dropped items.

### `InventoryManager`

File:

`Assets/Scripts/Player/Player/Inventory/InventoryManager.cs`

Current behavior:

- `AddItem(ItemInstance itemInstance, int quantity)`
- `RemoveItem(ItemInstance itemInstance, int quantity)`
- `NotifyInventoryUpdated()`

Important issue:

`RemoveItem(ItemInstance, int)` removes matching items from the inventory globally. For dropping, we usually want to remove from the exact clicked slot. Dropping from an exact slot should use `InventorySlot.DecreaseCount()` or `InventorySlot.Clear()` after validating that the slot belongs to the expected inventory.

### `UIInventoryEventsSO`

File:

`Assets/Scripts/UIToolkit/UI/Events/UIInventoryEventsSO.cs`

Current behavior:

- UI sends inventory action requests through this event asset.
- Backend controllers perform the actual inventory mutations.

The drop system should follow the same pattern.

## Recommended Flow

1. UI requests a drop.
2. Backend validates the request.
3. Backend clones/captures the item instance before mutating the slot.
4. Backend spawns a `WorldItem` near the player.
5. Backend removes the quantity from the exact source slot.
6. Backend notifies inventory UI.
7. The dropped item can later be picked up by `ItemPicker`.

## Event API

Add a new request event to `UIInventoryEventsSO`.

Recommended shape:

```csharp
public System.Action<InventoryManager, InventorySlot, int> OnRequestDropItem;
```

Why include `InventoryManager`:

- It lets the backend verify the slot came from the player backpack.
- It lets the backend verify the slot came from an allowed player-owned inventory.
- It avoids guessing which inventory owns the slot.
- It matches the existing drag/move event style.
- It prevents accidental drops from shop, crafting proxy, or external container slots.

Drag-out UI use:

```csharp
_uiInventoryEvents.OnRequestDropItem?.Invoke(sourceInventory, sourceSlot, sourceSlot.Count);
```

Hotkey while hovering use:

```csharp
_uiInventoryEvents.OnRequestDropItem?.Invoke(sourceInventory, hoveredSlot, 1);
```

Shift plus hotkey use:

```csharp
// UI opens amount chooser first.
_uiInventoryEvents.OnRequestDropItem?.Invoke(sourceInventory, hoveredSlot, chosenQuantity);
```

## Backend Owner

Use one of these options:

### Option A: Add To `InventoryActionController`

This is the smallest change because it already owns equip/use/unequip requests.

Good for first pass.

### Option B: Create `PlayerItemDropController`

This is cleaner if dropping grows into its own feature.

Recommended if you expect:

- drop positioning rules
- pickup delay rules
- sound effects
- item throw animation
- multiplayer ownership later

Either way, the controller should listen to:

```csharp
_uiInventoryEvents.OnRequestDropItem += HandleRequestDropItem;
```

and unsubscribe in `OnDisable()`.

## Drop Validation

The backend should reject the request if:

- source inventory is null
- source slot is null
- source slot is empty
- requested quantity is less than or equal to 0
- requested quantity is greater than the slot count
- source inventory is not one of the allowed player-owned inventories
- item is marked non-droppable later

First pass can skip a non-droppable item flag, but leave the validation method shaped for it.

Recommended helper:

```csharp
private bool CanDropItem(InventoryManager sourceInventory, InventorySlot sourceSlot, int quantity)
```

## Runtime Item State

This is the most important correctness detail.

`WorldItem` currently reconstructs pickup items from `InventoryItemSO`. That is okay for enemy loot, but it is not enough for player drops.

Add a runtime instance field:

```csharp
private ItemInstance _runtimeItem;
private bool _reportQuestPickupFact = true;
```

Keep the existing blueprint initializer for authored drops and enemy loot:

```csharp
public void Initialize(InventoryItemSO itemData, int quantity)
{
    _runtimeItem = null;
    _itemData = itemData;
    _quantity = Mathf.Max(1, quantity);
    _reportQuestPickupFact = true;
    ApplyVisuals();
}
```

Add a player-drop initializer:

```csharp
public void InitializeDroppedItem(ItemInstance itemInstance, int quantity)
{
    _runtimeItem = itemInstance != null ? itemInstance.Clone() : null;
    _itemData = _runtimeItem != null ? _runtimeItem.BaseItem : null;
    _quantity = Mathf.Max(1, quantity);
    _reportQuestPickupFact = false;
    ApplyVisuals();
}
```

Then pickup should use:

```csharp
ItemInstance item = _runtimeItem != null
    ? _runtimeItem.Clone()
    : new ItemInstance(_itemData);
```

Why clone:

- The inventory owns its item instances.
- The world pickup should not hand the same object reference around.
- `ItemInstance.Clone()` already deep-clones component states.

## Quest Pickup Facts

Player-dropped items should not report normal pickup quest facts when picked back up.

Otherwise a player could:

1. pick up an item once
2. drop it
3. pick it up again
4. repeat quest pickup progress forever

For first pass:

- Existing authored/loot `WorldItem` pickups should keep reporting facts.
- Player-dropped `WorldItem` pickups should skip quest fact reporting.

Implementation idea:

```csharp
if (_reportQuestPickupFact)
{
    ReportQuestPickUpFactIfNeeded();
}
```

## Removing From The Exact Slot

Do not use `InventoryManager.RemoveItem()` for the actual drop transaction unless you intentionally want global matching removal.

Dropping from a UI slot should mutate that exact slot:

```csharp
if (sourceSlot.Count > dropQuantity)
{
    sourceSlot.DecreaseCount(dropQuantity);
}
else
{
    sourceSlot.Clear();
}

sourceInventory.NotifyInventoryUpdated();
```

Cache everything needed before mutating:

```csharp
ItemInstance droppedItem = sourceSlot.HeldItem.Clone();
int dropQuantity = Mathf.Min(quantity, sourceSlot.Count);
```

This avoids bugs where the slot clears before the drop object is initialized.

## Input Quantity Rules

The backend should not care why the drop was requested. It should only receive a source slot and a quantity.

Quantity should be chosen by the caller:

- drag outside inventory: `sourceSlot.Count`
- hotkey while hovering: `1`
- shift plus hotkey while hovering: amount chooser result

The backend still clamps and validates the requested quantity before dropping.

## Spawn Position

First pass should keep this simple.

Recommended serialized fields:

```csharp
[SerializeField] private Transform _dropOrigin;
[SerializeField] private float _dropDistance = 0.85f;
[SerializeField] private float _dropScatterRadius = 0.2f;
```

Position rule:

- Use `_dropOrigin.position` if assigned.
- Otherwise use the player transform position.
- Add a small offset in the player's facing direction.
- Add tiny random scatter so repeated drops do not stack exactly.

If facing direction is awkward to access, use the player interaction point or last non-zero movement direction as the first-pass facing source.

## Prevent Immediate Re-Pickup

This is easy to miss.

If a dropped item spawns inside pickup range and has `WorldItemMagnet`, it may immediately fly back into the player inventory.

First pass rule:

- Do not add `WorldItemMagnet` to player-dropped items.

Optional polish if manual pickup feels too twitchy:

- give the dropped item's collider a short pickup delay, around `0.25f` to `0.5f`

This delay is not required to satisfy the first-pass rule if player-dropped items have no magnet.

## Spawn Object Shape

You can either instantiate a prefab or create the object in code.

### Prefab Option

Prefab contains:

- `WorldItem`
- `SpriteRenderer`
- `CircleCollider2D`
- correct item layer

The drop controller:

- instantiates prefab
- calls `InitializeDroppedItem()`
- sets position
- does not add or enable `WorldItemMagnet`

### Code Spawn Option

Use `EnemyLootRuntime.SpawnWorldItemDrop()` as reference.

The player drop version can be much smaller:

- create `GameObject`
- set layer to `Item`
- add `SpriteRenderer`
- add `CircleCollider2D`
- add `WorldItem`
- initialize from cloned `ItemInstance`
- do not add `WorldItemMagnet`

Longer term, enemy loot and player drop spawning could share a `WorldItemSpawnFactory`, but that is not required for the first pass.

## Transaction Order

Recommended order:

1. Validate request.
2. Clone item instance.
3. Clamp quantity.
4. Spawn and initialize world item.
5. Remove quantity from exact source slot.
6. Notify inventory updated.

If spawn fails, do not remove the item.

Do not remove first unless you also implement rollback.

## Minimal Handler Shape

```csharp
private void HandleRequestDropItem(InventoryManager sourceInventory, InventorySlot sourceSlot, int quantity)
{
    ResolveRuntimeReferences();

    if (!CanDropItem(sourceInventory, sourceSlot, quantity))
        return;

    int dropQuantity = Mathf.Min(quantity, sourceSlot.Count);
    ItemInstance droppedItem = sourceSlot.HeldItem.Clone();

    if (!TrySpawnDroppedWorldItem(droppedItem, dropQuantity))
        return;

    if (sourceSlot.Count > dropQuantity)
        sourceSlot.DecreaseCount(dropQuantity);
    else
        sourceSlot.Clear();

    sourceInventory.NotifyInventoryUpdated();
}
```

## UI Responsibility

The UI should only dispatch the request.

It should not:

- remove the item
- decrement the slot
- spawn the world item
- decide whether the item can be dropped

Example UI call:

```csharp
_uiInventoryEvents.OnRequestDropItem?.Invoke(sourceInventory, sourceSlot, sourceSlot.Count);
```

Input mapping:

- drag-out should call the event with the full slot count
- normal hotkey should call the event with `1`
- shift plus hotkey should open amount selection first, then call the event with the selected amount

## First Pass Acceptance Checklist

- Dropping an empty slot does nothing.
- Dragging outside inventory drops the full slot quantity.
- Hotkey while hovering drops 1 item.
- Shift plus hotkey while hovering drops the selected amount.
- Dropping a partial stack leaves the remaining quantity in the original slot.
- Dropping a non-stackable item clears only that slot.
- Dropping from backpack, potion slots, and equipment slots works.
- Dropped item appears in the world with the correct sprite.
- Dropped item does not instantly return to the player inventory.
- Player-dropped items do not use `WorldItemMagnet`.
- Player can pick the dropped item back up with the existing pickup input.
- Picked-up dropped item preserves `ItemInstance` runtime state.
- Picking up a player-dropped item does not increment pickup quest progress.
- Inventory UI refreshes after the drop.
- Dropped items do not need to survive scene reloads or procedural world regeneration.
- Shop/crafting/proxy slots cannot be dropped unless intentionally wired later.

## Suggested Implementation Order

1. Extend `WorldItem` to support dropped runtime `ItemInstance` data.
2. Add `OnRequestDropItem` to `UIInventoryEventsSO`.
3. Add backend drop handling in `InventoryActionController` or a new `PlayerItemDropController`.
4. Add world item spawn helper.
5. Omit `WorldItemMagnet` from player drops.
6. Wire drag-out, hotkey, and shift-hotkey UI actions to the event.
7. Test full-stack, one-item, selected-amount, equipment, potion, and non-stackable drops.

## Future Improvements

- Split stack popup.
- Drop confirmation for rare items.
- Non-droppable item flag on item data.
- Save dropped world items if needed.
- Shared `WorldItemSpawnFactory` for enemy loot and player drops.
- Drop sound and small throw animation.
