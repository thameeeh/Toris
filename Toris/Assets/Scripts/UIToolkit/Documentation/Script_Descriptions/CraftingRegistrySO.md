Identifier: OutlandHaven.UIToolkit.CraftingRegistrySO : ScriptableObject

Architectural Role: Singleton Manager / Data Container (Database for Recipes)

Core Logic (The 'Contract'):
- Abstract/Virtual Methods: None
- Public API:
  - GetSalvageRecipeFor(InventoryItemSO): Returns the SalvageRecipeSO matching the target item, or null if not found.
  - GetCraftingRecipeFor(InventoryItemSO): Returns the CraftingRecipeSO matching the base item requirement, or null if not found.

Dependency Graph (Crucial for Scaling):
- Upstream: Depends on CraftingRecipeSO, SalvageRecipeSO, InventoryItemSO.
- Downstream: Queried by CraftingManagerSO, SalvageManagerSO, UpgradeSalvageManagerSO, ForgeSubView, SalvageSubView.

Data Schema:
- List<CraftingRecipeSO> CraftingRecipes -> Collection of all transformative crafting recipes.
- List<SalvageRecipeSO> SalvageRecipes -> Collection of all salvage recipes.

Side Effects & Lifecycle:
- Lifecycle: Inspector-initialized data asset. Acts as a centralized, read-only lookup table at runtime.
- Memory: Does not allocate new objects. Returns references to existing ScriptableObjects.