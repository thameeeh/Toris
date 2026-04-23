Identifier: OutlandHaven.UIToolkit.SalvageManagerSO : ScriptableObject

Architectural Role: Singleton Manager / Event Arbitrator

Core Logic (The 'Contract'):
- Abstract/Virtual Methods: None
- Public API:
  - Initialize(): Subscribes to InventoryEvents.OnRequestSalvage.
  - Cleanup(): Unsubscribes from InventoryEvents.OnRequestSalvage.
  - CanSalvage(InventoryItemSO itemType): Returns true if player inventory contains salvageable instances of the item type.

Dependency Graph (Crucial for Scaling):
- Upstream: Depends on GameSessionSO, PlayerHUDBridge, UIInventoryEventsSO, CraftingRegistrySO, SalvageRecipeSO.
- Downstream: Subscribed to by UI/Game state initializers.

Data Schema:
- GameSessionSO SessionData -> Global session state referencing player inventory.
- PlayerHUDBridge _playerHudBridge -> Reference to active player progression/gold.
- UIInventoryEventsSO InventoryEvents -> Event channel for salvage requests.
- CraftingRegistrySO Registry -> Contains mappings from item types to SalvageRecipeSOs.

Side Effects & Lifecycle:
- Manual initialization via Initialize() and Cleanup().
- HandleRequestSalvage modifies PlayerInventory (removes item, adds materials) and PlayerAnchor (adds gold).
- Invokes  and OnInventoryUpdated events.
