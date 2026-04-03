Identifier: OutlandHaven.UIToolkit.MageScreenController : MonoBehaviour

Architectural Role: Component Logic

Core Logic:
- Abstract/Virtual Methods: None
- Public API: None

Dependency Graph:
- Upstream: Depends on UIManager, VisualTreeAsset (UI templates), UIEventsSO, UIInventoryEventsSO, GameSessionSO, ShopManagerSO, MageView.
- Downstream: None.

Data Schema:
- VisualTreeAsset _mageMainTemplate -> Mage screen root UI template.
- VisualTreeAsset _slotTemplate -> Reusable item slot template.
- VisualTreeAsset _shopTemplate -> Shop sub-view UI template.
- UIEventsSO _uiEvents -> Global UI event channel.
- UIInventoryEventsSO _uiInventoryEvents -> Inventory-specific UI event channel.
- GameSessionSO _gameSession -> Reference to global game session state.
- ShopManagerSO _shopManagerSO -> Shop transaction and logic manager.

Side Effects & Lifecycle:
- Awake: Queries the scene for UIManager singleton, and initializes _shopManagerSO.
- OnEnable/OnDisable: Subscribes/unsubscribes HandleRequestOpen to _uiEvents.OnRequestOpen and validates references.
- Start: Instantiates _mageMainTemplate, allocates and initializes a new MageView instance, and registers the view to UIManager on ScreenZone.Left.
- Event Callbacks: HandleRequestOpen validates payload as an InventoryManager for ScreenType.Mage, and dynamically injects the target NPC inventory into _shopManagerSO.CurrentShopInventory.
