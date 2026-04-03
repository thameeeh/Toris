Identifier: OutlandHaven.UIToolkit.UpgradeSalvageManagerSO : ScriptableObject

Architectural Role: Singleton Manager / Component Logic

Core Logic (The 'Contract'):
- Abstract/Virtual Methods: None
- Public API:
  - CalculateUpgradeCost(ItemInstance itemInstance): Returns integer gold cost based on item's current upgrade level.
  - TryUpgradeItem(InventorySlot slot): Validates conditions, deducts gold, increments item level, and notifies state change. Returns boolean success.
  - TrySalvageItem(InventoryManager container, ItemInstance itemInstanceToSalvage): Removes item, yields gold (and potentially materials), returns boolean success.

Dependency Graph (Crucial for Scaling):
- Upstream: Depends on GameSessionSO, PlayerProgressionAnchorSO, CraftingRegistrySO, SalvageRecipeSO, UpgradeableComponent, UpgradeableState.
- Downstream: Called by processing UI views/presenters (e.g., Forge/Salvage screens).

Data Schema:
- GameSessionSO SessionData -> Global session state.
- PlayerProgressionAnchorSO PlayerAnchor -> Reference to active player progression/gold.
- CraftingRegistrySO Registry -> Reference to recipes for salvage yields.
- int UpgradeBaseGoldCost -> Base multiplier for calculating upgrade costs.

Side Effects & Lifecycle:
- Executes on demand (no Update loop).
- TryUpgradeItem mutates ItemInstance state (increments CurrentLevel) and deducts gold via PlayerAnchor.
- TrySalvageItem mutates an InventoryManager (removes item) and adds gold via PlayerAnchor.
