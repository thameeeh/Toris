# Architectural Guide: GameSessionSO & Persistence Systems

## 1. Overview: The Central Brain
The `GameSessionSO` (ScriptableObject) is the primary architectural hub of the project. It acts as a **Single Source of Truth** for the player's runtime state. Because it exists in the Asset Domain, it persists across Unity scene loads, making it the "glue" that connects disparate systems (UI, Inventory, Stats, Save System).

### Key Responsibilities:
- **Global Service Locator:** Holds memory pointers to active scene instances (e.g., `PlayerInventory`).
- **Short-Term Memory:** Manages "Snapshots" of volatile data during scene transitions.
- **Long-Term Memory:** Coordinates the serialization of data to and from the hard drive.

---

## 2. Data Structure & Ownership

### A. Runtime References (Pointers)
The `GameSessionSO` holds references to live `MonoBehaviour` components currently in the scene.
- **Fields:** `PlayerInventory`, `PlayerEquipment`, `PlayerPotionInventory`.
- **Constraint:** These fields are marked `[System.NonSerialized]`. They live only in RAM and are never saved into the ScriptableObject asset file itself.
- **Safety Rule:** These references are "Volatile." They are cleared on `OnDisable` by the source components to prevent "MissingReferenceException" (Dangling Pointers) when a scene unloads.

### B. Progression & Stats
- **Health/Stamina:** Managed via `PlayerStatsAnchorSO` to decouple the UI from the Player GameObject.
- **Gold/XP:** Stored in the session to ensure currency carries over between expeditions.

---

## 3. The Snapshot System (Scene Transitions)
The Snapshot system allows data to survive the destruction of a scene.

### The Lifecycle:
1.  **Capture (Scene Exit):** When an `InventoryManager` (e.g., the Backpack) is disabled due to a scene transition, it calls `CaptureTransferredState()`. The data is copied into a `RuntimeSnapshot` within the `GameSessionSO`.
2.  **Storage:** The snapshot stays in memory while Unity loads the new scene.
3.  **Restore (Scene Enter):** The new `InventoryManager` in the destination scene wakes up and calls `TryRestoreTransferredState()`. It pulls the data from the snapshot and populates its slots.

### Use Cases:
- **Scene-to-Scene:** Keeping items while moving from "Town" to "Dungeon."
- **Main Menu Load:** Acting as a "Staging Area" for data read from a JSON file before the player object has spawned.

---

## 4. The Save System (Disk Persistence)

### Architecture:
- **SaveManager:** Handles file I/O (reading/writing strings to `Application.persistentDataPath`).
- **SaveDataOrchestrator:** The "Logic Engine" of the save system. It converts live session data into a serializable `GameSaveData` DTO (Data Transfer Object).
- **Newtonsoft.Json:** Used for high-level serialization, specifically to handle `[SerializeReference]` for item components and states.

### Workflow:
1.  **Save:** Orchestrator pulls data from the live `InventoryManager` instances (if active) or the current snapshots. It bundles this into a `GameSaveData` object.
2.  **Load:** Orchestrator takes a `GameSaveData` object and populates the `GameSessionSO` snapshots. When the gameplay scene loads, the standard Snapshot Restoration logic takes over.

---

## 5. Usage Guidelines & Safety

### Avoiding the "Prefab Trap"
**Never assign a Prefab Asset to a serialized field meant for a Scene Instance.**
- We have implemented **Strict Global Registration**. 
- **Incorrect:** Assigning an `InventoryManager` prefab to a controller in the Inspector.
- **Correct:** Controllers should call `ResolveRuntimeReferences()` to fetch the active instance from `GameSessionSO`.

### Registering New Global Systems
If you add a new global system (e.g., a "Skill Tree"):
1.  Add a reference field in `GameSessionSO`.
2.  In your new manager's `OnEnable`, assign `GlobalSession.MyNewSystem = this;`.
3.  In `OnDisable`, assign `GlobalSession.MyNewSystem = null;`.
4.  Update `SaveDataOrchestrator` to include your new data in the `GameSaveData` DTO.

### Timing & Race Conditions
`InventoryManager` registration happens in `OnEnable`. UI and Logic Controllers should ideally use `Start()` or a semantic "Initialization" event (like `OnSystemInitializationComplete`) to ensure the global references are fully registered and ready for use.
