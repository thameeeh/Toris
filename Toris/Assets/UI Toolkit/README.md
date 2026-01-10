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
      * Think of it as a single "Folder" that holds the current player's entire existence.
      * Instead of your UI needing separate references for HP, Mana, Backpack, Stash, and Save Slots, it only needs one reference to GameSessionSO. Through this hub, it can access everything else.
* **In Short**:
   * It is the persistent memory of the game. It ensures that no matter which scene triggers Start(), the game knows exactly who the player is and what they are carrying.

## PlayerDataSO

It holds runtime information for player stats such as Health, Mana, and Gold. It uses Events for variable changes instead of an `Update()` loop, ensuring the UI only repaints when values actually change.
It is responsible for player stat logic. Any system that inflicts damage or gives gold (e.g., an Enemy) must hold a reference to this `PlayerDataSO` asset and call the appropriate method (e.g., `ModifyHealth`).

<img width="919" height="248" alt="image" src="https://github.com/user-attachments/assets/c139a54c-4747-4259-8d0f-08ebc4fa4080" />
