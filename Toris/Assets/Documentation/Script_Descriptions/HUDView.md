- **Identifier:** `OutlandHaven.UIToolkit.HUDView : GameView`
- **Architectural Role:** Component Logic / Presentation Layer for Player HUD.

- **Core Logic (The 'Contract'):**
  - **Abstract/Virtual Methods:**
    - `ID` (Property): Implements `ScreenType.HUD`.
    - `Setup(object payload)`: Overridden to instantiate dynamic menu buttons and push initial data from the `PlayerHUDBridge` exactly once (`_isSetup` flag).
    - `SetVisualElements()`: Overridden to bind UI progress bars, labels, and menu containers.
    - `RegisterButtonCallbacks()`: Overridden to hook up the main menu toggle button.
    - `Show()`: Overridden to subscribe to `PlayerHUDBridge` stat events and trigger initial state push.
    - `Hide()`: Overridden to unsubscribe from `PlayerHUDBridge` events.
  - **Public API:**
    - `HUDView(...)`: Constructor for dependency injection (`PlayerHUDBridge`, `UIEventsSO`, button `VisualTreeAsset`).

- **Dependency Graph (Crucial for Scaling):**
  - **Upstream:**
    - Requires `OutlandHaven.UIToolkit.GameView` (Base Class).
    - Requires `OutlandHaven.UIToolkit.PlayerHUDBridge` (Data source and event channel for player stats).
    - Requires `UnityEngine.UIElements.VisualTreeAsset` (Menu button template).
  - **Downstream:**
    - Handled by global `UIManager`.

- **Data Schema:**
  - `_playerHudBridge` (PlayerHUDBridge): Data connection for health, stamina, XP, gold, and level.
  - `_buttonTemplate` (VisualTreeAsset): Template for generating quick-access menu buttons.
  - `PROGRESS_BAR_MAX` (const float = 100f): Scale factor for UI Toolkit progress bars.
  - `_isSetup` (bool): Flag preventing duplicate initialization.

- **Side Effects & Lifecycle:**
  - **Lifecycle:** Standard manual initialization lifecycle driven by `UIManager`.
  - **Side Effects:** Dynamically instantiates multiple UI elements into `_optionsContainer` during `Setup()`. Toggles inline UI display styles (`Flex`/`None`) upon button clicks. Modifies progress bar `value` attributes based on normalized bridging data.
