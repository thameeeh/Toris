Identifier: OutlandHaven.Inventory.ItemComponentState : System.Object

Architectural Role: Abstract Runtime State Data

Core Logic (The 'Contract'):
- Abstract/Virtual Methods:
  - IsStackableWith(ItemComponentState other) -> MUST be overridden. Defines equivalence logic for inventory stacking.
  - Clone() -> MUST be overridden. Returns a deep copy of the state data.
- Public API: None

Dependency Graph (Crucial for Scaling):
- Upstream: None.
- Downstream: Base class inherited by all runtime component states (e.g., `UpgradeableState`). Validated by inventory slot merge arbitrators.

Data Schema: None

Side Effects & Lifecycle:
- Abstract contract requires child classes to handle cloning allocations. Evaluated dynamically during inventory merge transactions.
