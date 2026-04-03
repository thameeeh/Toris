Identifier: OutlandHaven.UIToolkit.ShopManagerSO : ScriptableObject

Architectural Role: Singleton Manager / Event Arbitrator

Core Logic (The 'Contract'):
- Abstract/Virtual Methods: None
- Public API:
  - Initialize(): Subscribes to UIInventoryEventsSO (buy/sell) and UIEventsSO (screen open).
  - Cleanup(): Unsubscribes from events.

Dependency Graph (Crucial for Scaling):
- Upstream: Depends on GameSessionSO, PlayerProgressionAnchorSO, UIInventoryEventsSO, UIEventsSO, InventoryManager (NPC).
- Downstream: Subscribed to by UI/Game state initializers.

Data Schema:
- GameSessionSO SessionData -> Global session state referencing player inventory.
- PlayerProgressionAnchorSO PlayerAnchor -> Reference to active player progression/gold.
- UIInventoryEventsSO InventoryEvents -> Event channel for buy/sell requests.
- UIEventsSO UIEvents -> Event channel for tracking active shop views.
- InventoryManager CurrentShopInventory -> Non-serialized dynamic reference to the active vendor's inventory.

Side Effects & Lifecycle:
- Manual initialization via Initialize() and Cleanup().
- HandleRequestOpen caches the NPC's InventoryManager when a Smith or Mage screen opens.
- HandleRequestBuy modifies PlayerInventory, CurrentShopInventory, and PlayerAnchor (deducts gold).
- HandleRequestSell modifies PlayerInventory, CurrentShopInventory, and PlayerAnchor (adds gold).
- Invokes OnCurrencyChanged, OnShopInventoryUpdated, and OnInventoryUpdated events.
