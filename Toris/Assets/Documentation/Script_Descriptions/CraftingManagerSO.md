Identifier: OutlandHaven.UIToolkit.CraftingManagerSO : ScriptableObject

Architectural Role: Component Logic / Global Event Listener for Forge Interactions

Core Logic (The 'Contract'):
- Abstract/Virtual Methods: None
- Public API:
  - Initialize(): Subscribes to Forge event.
  - Cleanup(): Unsubscribes from Forge event.
  - CanForge(CraftingRecipeSO, InventorySlot, InventorySlot, out int, out int): Validates if a recipe can be forged based on slot contents, item quantities, and gold.
  - GetMatchingRecipe(InventoryItemSO, InventoryItemSO): Finds a valid recipe from the Registry for the given item combination.

Dependency Graph (Crucial for Scaling):
- Upstream: Depends on GameSessionSO, PlayerProgressionAnchorSO, UIInventoryEventsSO, CraftingRegistrySO, InventoryItemSO, InventorySlot, ItemInstance, CraftingRecipeSO.
- Downstream: Typically invoked via UI events (UIInventoryEventsSO.OnRequestForge).

Data Schema:
- GameSessionSO SessionData -> Access to player inventory.
- PlayerProgressionAnchorSO PlayerAnchor -> Access to player gold/stats.
- UIInventoryEventsSO InventoryEvents -> Event bus for crafting requests and UI updates.
- CraftingRegistrySO Registry -> Database of all valid recipes.

Side Effects & Lifecycle:
- Manual Initialization: Relies on external calls to Initialize() and Cleanup() to manage event subscriptions.
- Side Effects: HandleRequestForge modifies SessionData.PlayerInventory (adds/removes items) and PlayerAnchor.Instance.CurrentGold. Invokes UIInventoryEventsSO.OnCurrencyChanged and OnInventoryUpdated. Instantiates ItemInstance objects.