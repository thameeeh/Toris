Identifier: OutlandHaven.Inventory.InventoryScreenController : MonoBehaviour

Architectural Role: Component Logic

Core Logic:
- Abstract/Virtual Methods: None
- Public API: None

Dependency Graph:
- Upstream: Depends on UIManager, VisualTreeAsset (UI templates), GameSessionSO, UIEventsSO, UIInventoryEventsSO, InventoryManager (equipment), PlayerInventoryView.
- Downstream: None.

Data Schema:
- VisualTreeAsset _inventoryMainTemplate -> Main inventory UI markup template.
- VisualTreeAsset _slotTemplate -> Reusable item slot template.
- GameSessionSO _gameSession -> Reference to global game session state.
- UIEventsSO _uiEvents -> Global UI event channel.
- UIInventoryEventsSO _uiInventoryEvents -> Inventory-specific UI event channel.
- InventoryManager _equipmentInventory -> Configured equipment inventory container.

Side Effects & Lifecycle:
- Awake: Queries the scene for UIManager singleton.
- OnEnable/OnValidate: Editor and runtime validation of required references.
- Start: Instantiates _inventoryMainTemplate, mutates flexGrow style, allocates and initializes a new PlayerInventoryView instance, and registers the view to UIManager on ScreenZone.Right.
