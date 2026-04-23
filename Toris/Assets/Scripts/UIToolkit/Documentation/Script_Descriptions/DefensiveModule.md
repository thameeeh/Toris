Identifier: OutlandHaven.Inventory.DefensiveComponent : ItemComponent
Architectural Role: Abstract Blueprint / Data Container

Core Logic:
- Abstract/Virtual Methods: N/A (Inherits default behavior)
- Public API: N/A

Dependency Graph:
- Upstream:
  - `OutlandHaven.Inventory.ItemComponent`
- Downstream:
  - Evaluated by Player Equipment/Stat systems (e.g. `PlayerEffectResolver`) to apply damage mitigation.

Data Schema:
- `float PhysicalDefense` -> Flat physical damage reduction.
- `float MagicalDefense` -> Flat magical/elemental damage reduction.

Side Effects & Lifecycle:
- Pure data class. No custom initialization or state tracking. Relies entirely on parent `ItemComponent` lifecycle.
