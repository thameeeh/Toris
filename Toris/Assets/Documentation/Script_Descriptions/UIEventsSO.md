Identifier: OutlandHaven.UIToolkit.UIEventsSO : ScriptableObject

Architectural Role: Decoupled Event Channel / Event Bus

Core Logic (The 'Contract'):
- Abstract/Virtual Methods: None
- Public API: None (Exposes public UnityAction delegates for invocation/subscription).

Dependency Graph (Crucial for Scaling):
- Upstream: Depends on ScreenType (Enum).
- Downstream: Subscribed to by UIManager and UI Controllers. Invoked by UI interactions (e.g., buttons, hotkeys) or system logic.

Data Schema:
- UnityAction<ScreenType, object> OnRequestOpen -> Requests opening a specific screen, optionally passing a data payload (e.g., InventoryManager).
- UnityAction<ScreenType> OnRequestClose -> Requests closing a specific screen.
- UnityAction OnRequestCloseAll -> Requests closing all non-HUD screens.
- UnityAction<ScreenType> OnScreenOpen -> Broadcast when a screen successfully opens (duplicates static `UIEvents.OnScreenOpen`).

Side Effects & Lifecycle:
- Asset-based lifecycle. Delegates are bound/unbound at runtime by observing MonoBehaviours (e.g., UIManager). No internal execution logic or state mutations.
