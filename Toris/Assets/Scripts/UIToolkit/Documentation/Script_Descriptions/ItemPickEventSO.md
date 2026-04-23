Identifier: OutlandHaven.Inventory.ItemPickEventSO : ScriptableObject

Architectural Role: Decoupled Event Channel

Core Logic (The 'Contract'):
- Abstract/Virtual Methods: None
- Public API: None

Dependency Graph (Crucial for Scaling):
- Upstream: None.
- Downstream: Observed by UI systems and audio/particle managers acting on item pickup events; triggered by world interactables.

Data Schema:
- Action OnItemPick -> C# event fired when an item is picked up from the world.

Side Effects & Lifecycle:
- Asset creation via `CreateAssetMenu`. Acts as a persistent, global event bus across scene loads.
