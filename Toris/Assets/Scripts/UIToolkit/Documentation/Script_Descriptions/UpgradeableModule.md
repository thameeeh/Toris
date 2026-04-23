Identifier: OutlandHaven.Inventory.UpgradeableComponent : OutlandHaven.Inventory.ItemComponent

Architectural Role: Abstract Blueprint & Blueprint Component

Core Logic (The 'Contract'):
- Abstract/Virtual Methods:
  - CreateInitialState() -> Overridden to instantiate and return a new `UpgradeableState` initialized at level 1.
- Public API: None

Dependency Graph (Crucial for Scaling):
- Upstream: Depends on OutlandHaven.Inventory.ItemComponent.
- Downstream: Instantiates `UpgradeableState`. Read by upgrade mechanics to cap item leveling.

Data Schema:
- int MaxLevel -> Defines the maximum upgrade level constraint.

Side Effects & Lifecycle:
- Allocates to the managed heap via `CreateInitialState()` to generate the runtime state container.

---

Identifier: OutlandHaven.Inventory.UpgradeableState : OutlandHaven.Inventory.ItemComponentState

Architectural Role: Runtime State Data

Core Logic (The 'Contract'):
- Abstract/Virtual Methods:
  - IsStackableWith(ItemComponentState other) -> Evaluates if another `UpgradeableState` has an identical `CurrentLevel`.
  - Clone() -> Returns a new heap-allocated copy of the current state.
- Public API: None

Dependency Graph (Crucial for Scaling):
- Upstream: Depends on OutlandHaven.Inventory.ItemComponentState.
- Downstream: Modified by upgrade mechanic systems. Evaluated by inventory stacking arbitrators.

Data Schema:
- int CurrentLevel -> Tracks the current dynamic level of the specific item instance.

Side Effects & Lifecycle:
- Cloned state and initial state allocation generates heap allocations.
