Identifier: OutlandHaven.Inventory.EquipableComponent : ItemComponent

Architectural Role: Abstract Blueprint / Component Logic

Core Logic (The 'Contract'):
- Abstract/Virtual Methods: None
- Public API: None (Pure Data Container)

Dependency Graph (Crucial for Scaling):
- Upstream: Depends on EquipmentSlot (Enum).
- Downstream: Read by PlayerEquipmentController, PlayerEffectType, and PlayerEffectResolver.

Data Schema:
- EquipmentSlot TargetSlot -> Determines the valid inventory equipment slot (0=Head, 1=Chest, 2=Legs, 3=Arms, 4=Weapon).
- float StrengthBonus -> Numeric modifier applied to player strength.
- float DefenceBonus -> Numeric modifier applied to player defence.

Side Effects & Lifecycle:
- Pure data class (Serializable). No MonoBehaviour lifecycle (Update, FixedUpdate). No heap allocations during runtime.