Identifier: EquipmentEffectBridge : MonoBehaviour

Architectural Role: Component Logic / Decoupled Event Adapter

Core Logic (The 'Contract'):
- Abstract/Virtual Methods: None
- Public API:
  - void RefreshSlot(EquipmentSlot slot): Manually forces update for a specific equipment slot.
  - void RefreshAll(): Manually forces update across all equipment slots.

Dependency Graph (Crucial for Scaling):
- Upstream:
  - Requires `PlayerEquipmentController` (data source / event broadcaster).
  - Depends on `PlayerEffectSourceController` (stat effect receiver).
  - Depends on `ItemInstance` (item data and state events).
  - Depends on `EquippedItemEffectSource` (wrapper class for effects).
- Downstream:
  - Subscribes to `ItemInstance.OnStateChanged`.

Data Schema:
- PlayerEquipmentController _equipment -> Provides equipped item references and fires equip/unequip events.
- PlayerEffectSourceController _effectSourceController -> System handling active stat modifiers.
- Dictionary<EquipmentSlot, ItemInstance> _subscribedItems -> Caches tracked items to manage event lifecycle.

Side Effects & Lifecycle:
- Reset/Awake: Auto-assigns `_equipment` via `GetComponent`.
- OnEnable: Subscribes to `_equipment.OnEquippedItemChanged`.
- OnDisable: Unsubscribes from `_equipment` and all tracked `ItemInstance.OnStateChanged` events, then clears `_subscribedItems` dictionary.
- Start: Triggers `RefreshAll()` to apply initial loadout.
- HandleEquippedItemChanged: Allocates `EquippedItemEffectSource` on heap when equip happens. Adds/removes sources via `_effectSourceController`. Subscribes/unsubscribes to `ItemInstance.OnStateChanged`.
- HandleEquippedItemStateChanged: Listens to durability/upgrade changes on items and calls `RefreshSlot()`.
