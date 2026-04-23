Identifier: OutlandHaven.UIToolkit.ProgressionComponent : OutlandHaven.Inventory.ItemComponent

Architectural Role: Data Container / Blueprint Component

Core Logic (The 'Contract'):
- Abstract/Virtual Methods: None
- Public API: None

Dependency Graph (Crucial for Scaling):
- Upstream: Depends on OutlandHaven.Inventory.ItemComponent.
- Downstream: Queried by inventory and UI systems for sorting and filtering logic.

Data Schema:
- ProgressionCategory Category -> Defines item type (Material, QuestItem, Key, Junk) for sorting and filtering logic.

Side Effects & Lifecycle:
- Does not create a runtime state override (no heap allocation for state).
- Items are perfectly stackable by default.
