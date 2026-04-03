Identifier: OutlandHaven.Inventory.EvolvingComponent : ItemComponent

Architectural Role: Abstract Blueprint (Static Rules)

Core Logic (The 'Contract'):
- Abstract/Virtual Methods: CreateInitialState() -> Generates new EvolvingState.
- Public API: None

Dependency Graph (Crucial for Scaling):
- Upstream: None
- Downstream: Instantiates EvolvingState.

Data Schema:
- int KillsRequired -> Threshold for awakening.
- float AwakenedDamageBonus -> Flat damage modifier.

Side Effects & Lifecycle:
- Instantiates EvolvingState upon creation. No MonoBehaviour lifecycle.

---

Identifier: OutlandHaven.Inventory.EvolvingState : ItemComponentState

Architectural Role: Data Container / Runtime Tracker (Live Data)

Core Logic (The 'Contract'):
- Abstract/Virtual Methods:
  - IsStackableWith(ItemComponentState) -> Compares kill count and awakened state.
  - Clone() -> Returns a new instance with copied values.
- Public API:
  - AddKill(int requiredKills) -> Increments kills and updates awakened state if threshold is met.

Dependency Graph (Crucial for Scaling):
- Upstream: None
- Downstream: Mutated by combat events (e.g., entity death).

Data Schema:
- int CurrentKills -> Tracks progress.
- bool IsAwakened -> Tracks if max kills are met.

Side Effects & Lifecycle:
- Pure data class. Allocates on heap via Clone(). No MonoBehaviour lifecycle.