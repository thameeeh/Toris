Identifier: OutlandHaven.UIToolkit.CraftingRecipeSO : ScriptableObject

Architectural Role: Data Container / Abstract Blueprint for Transformative Crafting

Core Logic (The 'Contract'):
- Abstract/Virtual Methods: None
- Public API: None (Pure Data Container)

Dependency Graph (Crucial for Scaling):
- Upstream: Depends on InventoryItemSO, CraftingMaterialRequirement (Struct).
- Downstream: Referenced by CraftingRegistrySO, CraftingManagerSO, ForgeSubView.

Data Schema:
- InventoryItemSO BaseItemRequirement -> The primary item required for crafting.
- List<CraftingMaterialRequirement> MaterialRequirements -> Additional items and their quantities needed.
- int GoldCost -> Gold required to perform the craft.
- InventoryItemSO OutputItem -> The resulting item created by the recipe.

Side Effects & Lifecycle:
- Lifecycle: Inspector-initialized data asset. Does not use Unity update loops.
- Memory: Read-only at runtime. Does not allocate or instantiate.