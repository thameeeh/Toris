Identifier: OutlandHaven.UIToolkit.SmithScreenController : MonoBehaviour

Architectural Role: Component Logic

Core Logic:
- Abstract/Virtual Methods: None
- Public API: None

Dependency Graph:
- Upstream: Depends on UIManager, VisualTreeAsset (UI templates), UIEventsSO, UIInventoryEventsSO, GameSessionSO, PlayerProgressionAnchorSO, ShopManagerSO, CraftingManagerSO, SalvageManagerSO, SmithView.
- Downstream: None.

Data Schema:
- VisualTreeAsset _smithMainTemplate, _slotTemplate, _shopTemplate, _forgeTemplate, _salvageTemplate -> View hierarchy UI templates.
- UIEventsSO _uiEvents -> Global UI event channel.
- UIInventoryEventsSO _uiInventoryEvents -> Inventory-specific UI event channel.
- GameSessionSO _gameSession -> Reference to global game session state.
- PlayerProgressionAnchorSO _playerAnchor -> Player progression data anchor.
- ShopManagerSO _shopManagerSO -> Shop transaction manager.
- CraftingManagerSO _craftingManagerSO -> Forging logic manager.
- SalvageManagerSO _salvageManagerSO -> Salvage processing manager.

Side Effects & Lifecycle:
- Awake: Queries the scene for UIManager singleton, and initializes all linked logic managers (_shopManagerSO, _craftingManagerSO, _salvageManagerSO).
- OnEnable/OnDisable: Subscribes/unsubscribes HandleRequestOpen to _uiEvents.OnRequestOpen and validates references.
- Start: Instantiates _smithMainTemplate, allocates and initializes a new SmithView instance, and registers the view to UIManager on ScreenZone.Left.
- Event Callbacks: HandleRequestOpen validates payload as an InventoryManager for ScreenType.Smith, and dynamically injects the target NPC inventory into _shopManagerSO.CurrentShopInventory.
