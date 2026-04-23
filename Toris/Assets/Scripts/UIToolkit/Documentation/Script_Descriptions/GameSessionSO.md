Identifier: OutlandHaven.UIToolkit.GameSessionSO : ScriptableObject

Architectural Role: Singleton Manager / Data Container (Global Game State)

Core Logic (The 'Contract'):
- Abstract/Virtual Methods: None
- Public API: None (Pure Data Container)

Dependency Graph (Crucial for Scaling):
- Upstream: Depends on InventoryManager.
- Downstream: Referenced globally by various systems requiring access to the player's primary inventory or current save state (e.g., CraftingManagerSO, PlayerEquipmentController, UIManager).

Data Schema:
- InventoryManager PlayerInventory -> Non-serialized reference to the active player's inventory manager.
- int CurrentSaveSlotIndex -> Tracks the currently active save slot (0, 1, or 2).
- string targetSpawnPointID -> Identifier for where the player should spawn upon scene load.

Side Effects & Lifecycle:
- Lifecycle: Inspector-initialized data asset acting as a runtime global blackboard.
- Side Effects: PlayerInventory is injected at runtime (e.g., by the player's inventory component on Awake/OnEnable), making it a mutable global reference point.