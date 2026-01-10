1. System Overview

The User Interface (UI) system in OutlandHaven is designed to be decoupled, event-driven, and data-oriented. It avoids the common pitfall of the UI checking for updates every frame (Polling). Instead, it relies on an Observer Pattern where the UI reacts only when data changes, and a Payload Pattern to handle dynamic content (like chests or vendors) using a single, reusable code module.

2. Architecture
   A. UIManager - decides which window is open and handles the global "Stack" (e.g., pressing Escape closes the top window).
   B. UIEvents - static class defining Actions that anyone can shout into.
   C. Views - these are classes inheriting from GameView (e.g., InventoryView, HUDView). **Responsibility**: They bind directly to ScriptableObjects. They do not run game logic; they only display data.
