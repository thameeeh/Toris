Identifier: OutlandHaven.UIToolkit.SalvageRecipeSO : ScriptableObject

Architectural Role: Data Container / Blueprint

Core Logic (The 'Contract'):
- Abstract/Virtual Methods: None
- Public API: None

Dependency Graph (Crucial for Scaling):
- Upstream: Depends on InventoryItemSO (target item and yields), CraftingMaterialRequirement.
- Downstream: Read by SalvageManagerSO, UpgradeSalvageManagerSO, and CraftingRegistrySO.

Data Schema:
- InventoryItemSO TargetItem -> The input item to be salvaged.
- int GoldYield -> Base gold output from salvaging.
- List<CraftingMaterialRequirement> MaterialYields -> List of materials (and quantities) yielded from salvaging.

Side Effects & Lifecycle:
- Static data container. No runtime side effects or dynamic instantiation.
