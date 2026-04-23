Identifier: EquippedItemStatCalculator : static class

Architectural Role: Component Logic / Pure Function Calculator

Core Logic (The 'Contract'):
- Abstract/Virtual Methods: None (Static class)
- Public API:
  - EquippedItemComputedStats Calculate(ItemInstance item): Extracts component and dynamic state data from the item to assemble a unified stats struct.
  - WeaponComputedStats CalculateWeapon(ItemInstance item): Calls `Calculate` internally and applies specific math (e.g., strength multipliers, upgrade bonus logic) for weapons.

Dependency Graph (Crucial for Scaling):
- Upstream:
  - Depends on `ItemInstance` (base object).
  - Depends on `EquipableComponent`, `OffensiveComponent`, `DefensiveComponent`, `EvolvingComponent` (modular component data).
  - Depends on `UpgradeableState`, `EvolvingState` (dynamic runtime states).
  - Depends on `EquippedItemComputedStats`, `WeaponComputedStats` (return structs).
- Downstream:
  - Called by `EquippedItemEffectSource` (stat aggregation).
  - Can be called by UI (e.g., tooltip rendering).

Data Schema:
- No instance or static fields. Operates purely on provided arguments.

Side Effects & Lifecycle:
- Pure functions: Does not modify inputs, maintains no internal state, uses Unity's `GetComponent` strictly on `item.BaseItem` (reads only). No allocations (returns structs).
