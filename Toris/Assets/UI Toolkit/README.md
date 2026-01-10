# System Overview

The User Interface (UI) system in OutlandHaven is designed to be decoupled, event-driven, and data-oriented. It avoids the common pitfall of the UI checking for updates every frame (Polling). Instead, it relies on an Observer Pattern where the UI reacts only when data changes, and a Payload Pattern to handle dynamic content (like chests or vendors) using a single, reusable code module.

# Architecture

* **A. UIManager** - decides which window is open and handles the global "Stack" (e.g., pressing Escape closes the top window).
* **B. UIEvents** - static class defining Actions that anyone can shout into.
* **C. Views** - these are classes inheriting from GameView (e.g., InventoryView, HUDView).
    * **Responsibility:** They bind directly to ScriptableObjects. They do not run game logic; they only display data.

# The Data Layer (ScriptableObjects)

All persistent state is stored in ScriptableObjects. This acts as the "Shared Hard Drive" for the game. All SO stored in `Assets -> Resources`

# Workflow

### Scenario A: Health Bar updates without checking every frame.

1.  **Gameplay:** An Enemy hits the Player GameObject. The Player script calls `PlayerDataSO.ModifyHealth(-10)`.
2.  **Data:** The SO calculates the new health and fires public event `Action<float, float> OnHealthChanged`.
3.  **UI:** The HUDView, which subscribed to this event, wakes up.
4.  **Visuals:** HUDView updates the `.value` of the standard ProgressBar.

### Scenario B: One UI screen handles both the Player's Backpack and a random Chest.

1.  **Interaction:** Player walks into a Chest trigger zone and presses 'F'.
2.  **Trigger:** `WorldContainer.cs` on the chest grabs its own specific data (`Container_VillageChest`).
3.  **Event:** It fires `UIEvents.OnRequestOpen(ScreenType.Inventory, Container_VillageChest)`.
4.  **Manager:** `UIManager` receives the call and activates InventoryView, passing the "Chest Data" along.
5.  **View:** InventoryView checks the payload:
    * **If Payload exists:** It renders the "Right Panel" with the Chest's items.
    * **Always:** It renders the "Left Panel" with the Player's items (from `GameSessionSO`).

## GameSessionSO: The Runtime Data Hub

**GameSessionSO** is the central container for the active playthrough's state. It acts as a bridge between your static assets (code/prefabs) and the changing data of the current game (Health, Inventory).

   * **Scene Independence (Persistence)**:
      * Unlike GameObjects, which are destroyed when loading a new scene (e.g., moving from Village to Dungeon), ScriptableObjects live in memory.
      * GameSessionSO ensures that when a new scene loads, the UI and Gameplay scripts can immediately reconnect to the correct Health and Inventory data without losing progress.
   * **The "Session Folder" Metaphor**:
      * Holds current player's entire existence.
      * Instead of your UI needing separate references for HP, Mana, Backpack, Stash, and Save Slots, it only needs one reference to GameSessionSO. Through this hub, it can access everything else.
* **In Short**:
   * It is the persistent memory of the game. It ensures that no matter which scene triggers Start(), the game knows exactly who the player is and what they are carrying.

## PlayerDataSO

It holds runtime information for player stats such as Health, Mana, and Gold. It uses Events for variable changes instead of an `Update()` loop, ensuring the UI only repaints when values actually change.
It is responsible for player stat logic. Any system that inflicts damage or gives gold (e.g., an Enemy) must hold a reference to this `PlayerDataSO` asset and call the appropriate method (e.g., `ModifyHealth`).

## UIManager

It serves as the central "Brain" of the UI architecture, managing the lifecycle and visibility of all application windows.
   * **View Registry**: Holds a private list of every active `GameView`. It does not create views but waits for them to register themselves.
   * **Window Management**: Responsible for the logic of opening, closing, and toggling windows. It ensures only the appropriate windows are visible (e.g., closing Inventory when opening the Map).
   * **Input Handling**: In its `Update()` loop, it listens for global UI hotkeys (currently tracking 'I' for Inventory and 'C' for Character Sheet) and fires the corresponding events.
   * **Registration Process**: It exposes a public method void `RegisterView(GameView view)`. Every individual View Controller (like `InventoryScreenController`) calls this method during its own `OnEnable()` phase to add itself to the Manager's list.


*(Image) InventoryScreenController*
<img width="869" height="246" alt="Screenshot 2026-01-10 191710" src="https://github.com/user-attachments/assets/8127c605-0ba9-4bec-b9cc-2e95ae064f51" />


## Paylaod

Object of `InventoryContainerSO` Type is sent on `Action<ScreenType, object> OnRequestOpen` call. If it is `null`, only the player's Inventory is opened, otherwise the right side of the screen is filled with additional Inventory. Player and any other additional inventory-like objects use the `InventoryContainerSO`


<img width="623" height="187" alt="image" src="https://github.com/user-attachments/assets/81d5028c-dd50-4a06-bc05-4bc1c4a5e7e8" />


`UIMamager` opens only the Player's Inventory. Data flow `GameSessionSO` -> `InventoryView`.

<img width="639" height="114" alt="image" src="https://github.com/user-attachments/assets/7463ef01-db54-4cc0-bb16-3736fb242e3a" />


In any other container that have `WorldContainer` script and `InventoryContainerSO` asset is called Action that caries additional payload.

<img width="649" height="154" alt="image" src="https://github.com/user-attachments/assets/6ad5c287-1147-43d3-a6d7-fb9df80bd7d4" />
