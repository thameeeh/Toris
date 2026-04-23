Identifier: OutlandHaven.Inventory.ConsumableComponent : ItemComponent / OutlandHaven.Inventory.ConsumableState : ItemComponentState
Architectural Role: Abstract Blueprint (ConsumableComponent) / Data Container (ConsumableState)

Core Logic:
- Abstract/Virtual Methods:
  - `CreateInitialState()` [ConsumableComponent] -> Initializes and returns a new `ConsumableState` with `MaxCharges`.
  - `IsStackableWith(ItemComponentState other)` [ConsumableState] -> Evaluates if another item can stack. Requirement: Exact match on `CurrentCharges`.
  - `Clone()` [ConsumableState] -> Returns a deep copy of the current state.
- Public API: N/A (Data/Blueprint focus)

Dependency Graph:
- Upstream:
  - `OutlandHaven.Inventory.ItemComponent`
  - `OutlandHaven.Inventory.ItemComponentState`
  - `OutlandHaven.Inventory.ConsumptionSlot` (Enum)
- Downstream:
  - Consumed by Inventory System logic for item usage and stacking validation.
  - Utilized by Player Effect/Stat systems upon usage (Implied via `EffectPayload`).

Data Schema:
- Blueprint (ConsumableComponent):
  - `ConsumptionSlot EffectPayload` -> Defines target effect (HP, Mana).
  - `int amount` -> Restoration/Effect magnitude.
  - `float CooldownDuration` -> Time restriction between uses.
  - `int MaxCharges` -> Base maximum uses.
- Runtime State (ConsumableState):
  - `int CurrentCharges` -> Tracks remaining uses.

Side Effects & Lifecycle:
- Instantiates `ConsumableState` heap objects via `CreateInitialState()` and `Clone()`.
- Static rules defined in Blueprint; mutable state tracked in State class.
