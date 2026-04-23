Identifier: EquippedItemEffectSource : IPlayerEffectSource

Architectural Role: Data Container / Component Logic Adapter

Core Logic (The 'Contract'):
- Abstract/Virtual Methods: None (Sealed class)
- Public API:
  - string SourceKey: Read-only property returning the unique identifier string.
  - void CollectModifiers(List<PlayerEffectModifier> modifiers): Evaluates internal item and appends relevant offensive/defensive modifier structs into the provided list.

Dependency Graph (Crucial for Scaling):
- Upstream:
  - Implements `IPlayerEffectSource`.
  - Depends on `ItemInstance` (raw data).
  - Depends on `EquippedItemStatCalculator` (computes final values).
  - Depends on `PlayerEffectModifier` (structs to output).
- Downstream:
  - Passed by `EquipmentEffectBridge` to `PlayerEffectSourceController`.

Data Schema:
- string _sourceKey -> Identifier (e.g., "Equipment_Weapon").
- ItemInstance _item -> Reference to the item being evaluated.

Side Effects & Lifecycle:
- Constructor: Initializes fields without side effects.
- CollectModifiers: Invokes `EquippedItemStatCalculator.Calculate(_item)`. Adds `PlayerEffectModifier` structs (value types) to the provided list (no heap allocation for the structs themselves if using list backing array).
