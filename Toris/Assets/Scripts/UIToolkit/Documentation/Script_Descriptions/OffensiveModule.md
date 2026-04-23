Identifier: OutlandHaven.Inventory.OffensiveComponent : ItemComponent

Architectural Role: Abstract Blueprint (Static Rules)

Core Logic (The 'Contract'):
- Abstract/Virtual Methods: None
- Public API: None (Pure Data Container)

Dependency Graph (Crucial for Scaling):
- Upstream: None
- Downstream: Read by combat systems to calculate damage.

Data Schema:
- float BaseDamage -> The default static damage value.
- float AttackSpeed -> Attacks per second.

Side Effects & Lifecycle:
- Pure data class (Serializable). No runtime state generated. No MonoBehaviour lifecycle. No heap allocations during runtime.