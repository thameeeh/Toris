Identifier: OutlandHaven.Inventory.ItemComponent : System.Object

Architectural Role: Abstract Blueprint

Core Logic (The 'Contract'):
- Abstract/Virtual Methods:
  - CreateInitialState() -> Can be overridden by derived blueprint components to generate dynamic runtime state trackers. Returns null by default.
- Public API: None

Dependency Graph (Crucial for Scaling):
- Upstream: None.
- Downstream: Base class inherited by all item component definitions (e.g., `UpgradeableComponent`, `ProgressionComponent`). Read by inventory instantiation systems.

Data Schema: None

Side Effects & Lifecycle:
- Does not hold runtime data.
- Does not inherently allocate heap memory unless `CreateInitialState()` is overridden to do so by a child class.
