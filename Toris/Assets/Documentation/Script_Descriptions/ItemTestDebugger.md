Identifier: OutlandHaven.Inventory.EvolvingWeaponDebugger : MonoBehaviour

Architectural Role: Component Logic / Temporary Testing Script

Core Logic (The 'Contract'):
- Abstract/Virtual Methods: None
- Public API: None

Dependency Graph (Crucial for Scaling):
- Upstream: Depends on InventoryItemSO, EvolvingComponent, EvolvingState, ItemInstance.
- Downstream: None (Stand-alone testing script).

Data Schema:
- InventoryItemSO CursedDaggerBlueprint -> Blueprint used to instantiate the test item.

Side Effects & Lifecycle:
- Uses Unity Start() lifecycle method.
- Instantiates a new ItemInstance on the managed heap.
- Simulates state changes (adding kills) to test the evolving weapon logic.
- Logs output to the Unity console.
