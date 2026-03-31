# Hybrid Data Architecture

This document describes the architectural pattern used for player stats, progression, and UI synchronization in the game, specifically replacing the legacy `PlayerDataSO` approach.

## Core Philosophy

To keep systems clean, testable, and strictly single-responsibility, the game employs a "Hybrid Architecture" mixing Runtime Anchors with localized C# events.

1.  **Source of Truth:** MonoBehaviours (e.g., `PlayerProgression`, `PlayerStats`) act as the definitive state containers on the player prefab.
2.  **Global Access (Queries & Commands):** Global systems (e.g., `ShopManagerSO`, `CraftingManagerSO`) use Runtime Anchor ScriptableObjects to interact with the player without direct scene coupling.
3.  **UI Updates (Events):** The HUD and other UI layers do *not* use Anchors. Instead, they bind to a presentation bridge (`PlayerHUDBridge`) that aggregates C# events (`OnGoldChanged`, `OnHealthChanged`), ensuring a reactive and decoupled UI.

## Components

### 1. The Anchors (e.g., `PlayerProgressionAnchorSO`)
*   **What:** A simple ScriptableObject holding a `[System.NonSerialized]` reference to a specific type.
*   **Lifecycle:**
    *   The corresponding MonoBehaviour (`PlayerProgression`) calls `anchor.Provide(this)` in `OnEnable()`.
    *   It calls `anchor.Clear()` in `OnDisable()`.
*   **Usage:** Global managers inject these anchors via the Inspector. When a shop transaction occurs, the manager checks `PlayerAnchor.Instance.CurrentGold` and calls `PlayerAnchor.Instance.TrySpendGold(amount)`.

### 2. The Bridge (`PlayerHUDBridge`)
*   **What:** A MonoBehaviour attached to the player prefab that acts as a UI-facing facade.
*   **Responsibility:** It listens to internal player state components (`PlayerProgression`, `PlayerStats`, `PlayerStatusController`) and re-emits them in a presentation-friendly format.
*   **Usage:** The `HudScreenController` finds this bridge upon initialization and passes it to the `HUDView`. The view subscribes to the bridge's events, keeping the view entirely ignorant of gameplay logic and global managers.

## Deprecation Notice

`PlayerDataSO` has been entirely removed from the project.
*   Do not store mutable stats like Current Health or Gold directly in a ScriptableObject.
*   Do not use `GameSessionSO` to hold references to player stats. Use Anchors for Manager access and `PlayerHUDBridge` for UI.
