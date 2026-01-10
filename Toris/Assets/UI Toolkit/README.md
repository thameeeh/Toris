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

* **-> Gameplay:** An Enemy hits the Player GameObject. The Player script calls `PlayerDataSO.ModifyHealth(-10)`.
* **-> Data:** The SO calculates the new health and fires public event `Action<float, float> OnHealthChanged`.
* **-> UI:** The HUDView, which subscribed to this event, wakes up.
* **-> Visuals:** HUDView updates the `.value` of the standard ProgressBar.

### Scenario B: One UI screen handles both the Player's Backpack and a random Chest.

1.  **Interaction:** Player walks into a Chest trigger zone and presses 'F'.
2.  **Trigger:** `WorldContainer.cs` on the chest grabs its own specific data (`Container_VillageChest`).
3.  **Event:** It fires `UIEvents.OnRequestOpen(ScreenType.Inventory, Container_VillageChest)`.
4.  **Manager:** `UIManager` receives the call and activates InventoryView, passing the "Chest Data" along.
5.  **View:** InventoryView checks the payload:
    * **If Payload exists:** It renders the "Right Panel" with the Chest's items.
    * **Always:** It renders the "Left Panel" with the Player's items (from `GameSessionSO`).
