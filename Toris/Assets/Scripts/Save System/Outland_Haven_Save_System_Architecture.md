# Outland Haven: Save System Architecture & Data Flow

## Overview
The save system transitions the game from a volatile runtime state (Unity MonoBehaviours and ScriptableObjects) into a persistent, serialized state using **Newtonsoft.Json**. To avoid performance penalties, circular references, and Unity-specific asset pollution, the architecture utilizes the **Data Transfer Object (DTO)** pattern—often referred to as the "Stripped-Out Box" method.

Instead of serializing complex Unity objects directly, the system extracts pure C# data (ints, floats, strings, and polymorphic state lists), packages them into DTOs, and writes them to the hard drive. During the load process, this pure data is read and injected back into the active game components.

---

## 1. Data Flow: How Information Moves

The complete cycle of saving and loading follows a strict 3-step sequence: **Extract → Serialize → Restore**.

### The Save Sequence (F5)
1. **Trigger:** `SaveManager` receives the command.
2. **Gathering:** `SaveManager` calls `GameSessionSO.ExportToSaveData()`.
3. **Extraction:**
   - `GameSessionSO` checks if the player is currently in the scene using **Runtime Anchors** (`PlayerProgressionAnchorSO`, `PlayerStatsAnchorSO`).
   - If active, it reads live stats (HP, Stamina, Level, XP, Gold). If inactive, it uses memory snapshots.
   - It reads the `PlayerInventory` (Backpack) and `PlayerEquipment` managers, extracting the string IDs of items and their dynamic runtime `States` (e.g., durability, consumable charges).
4. **Packaging:** All this data is placed into the `GameSaveData` DTO.
5. **Serialization:** Newtonsoft.Json converts the DTO to a JSON string (handling polymorphic state classes via `TypeNameHandling.Auto`) and writes it to `quicksave.json`.

### The Load Sequence (F9)
1. **Trigger:** `SaveManager` receives the command and reads `quicksave.json` from the disk.
2. **Deserialization:** Newtonsoft converts the JSON text back into a `GameSaveData` object.
3. **Routing:** `SaveManager` passes the loaded data to `GameSessionSO.ImportFromSaveData()`.
4. **Restoration:**
   - `GameSessionSO` pushes Level, XP, Gold, HP, and Stamina back into the live `PlayerProgression` and `PlayerStats` components via `SetRuntimeState()`, which instantly updates the UI.
   - For items, the system reads the string ID from the save data, queries the `ItemDatabaseSO` for the static blueprint (`InventoryItemSO`), and reconstructs the `ItemInstance` with its saved polymorphic states.
   - The rebuilt items are pushed back into the `InventoryManager` slots.

---

## 2. Core Scripts and Responsibilities

### `SaveManager.cs`
* **Role:** The Executor.
* **Responsibility:** Listens for input (F5/F9) or UI calls. It handles all file I/O operations (reading/writing to `Application.persistentDataPath`) and configures Newtonsoft.Json. It strictly orchestrates the process but holds no game logic itself.

### `GameSaveData.cs` (and nested DTOs)
* **Role:** The "Stripped-Out Box" (Data Transfer Objects).
* **Responsibility:** Pure C# classes holding zero Unity Engine references. Contains fields for Progression, Stats, and Lists of `SavedSlotData`. This is the exact shape of the resulting JSON file.

### `GameSessionSO.cs`
* **Role:** The Bridge / Router.
* **Responsibility:** Acts as the central hub connecting the generic save system to the live game. It knows where the player's inventories and anchors are located. It executes the translation between Live Unity Objects and the pure `GameSaveData` DTO.

### `ItemDatabaseSO.cs`
* **Role:** The Master Registry.
* **Responsibility:** Since JSON only stores a string ID for an item (e.g., `"Sword_Iron_01"`), the database initializes a high-speed Dictionary at runtime. When loading, it instantly translates that string back into the correct `InventoryItemSO` asset blueprint so the item can be reconstructed.

### `InventoryManager.cs`
* **Role:** The Data Container.
* **Responsibility:** Holds the live `InventorySlot` data. We added extraction methods here so it can easily map its live items into `SavedSlotData` for the save file, and clear/repopulate its slots during a load.

### `PlayerProgression.cs` & `PlayerStats.cs`
* **Role:** The Source of Truth.
* **Responsibility:** Run the actual gameplay logic. We connected them to the save system using their `SetRuntimeState()` methods, ensuring that loading a save file immediately broadcasts events to update the health bars and experience UI.

---

## 3. What is Captured and Saved?

The current architecture successfully saves and restores the following data parameters:

* **Meta Data:** * `SaveTime` (Timestamp)
    * `CurrentSceneName` (Optional context)
    * `SpawnPointID` (Where the player should spawn on load)
* **Player Progression:** * `Level`, `Experience`, `Gold`
* **Player Stats:** * `CurrentHealth`, `CurrentStamina`
* **Inventories (Backpack & Equipment):**
    * `SlotIndex` (Preserving the exact layout of the UI)
    * `Count` (Stack sizes)
    * `InstanceID` (A GUID for tracking unique items)
    * `BaseItemID` (The string used to find the `InventoryItemSO` blueprint)
    * `States` (A polymorphic list storing the dynamic variables of specific item modules, such as `CurrentCharges` for consumables, ensuring an item's unique history is preserved).
