- **Identifier:** `OutlandHaven.UIToolkit.GameView : UIView`
- **Architectural Role:** Abstract Blueprint / Base Class for distinct full-screen UI views managed by `UIManager`.

- **Core Logic (The 'Contract'):**
  - **Abstract/Virtual Methods:**
    - `ID` (Abstract Property): Must be implemented by children to define their `ScreenType` identifier.
    - `Show()`: Overridden to invoke `UIEvents.OnScreenOpen` upon displaying the view, alongside base behavior.
  - **Public API:**
    - `GameView(...)`: Constructor for base element binding and `UIEventsSO` dependency injection.

- **Dependency Graph (Crucial for Scaling):**
  - **Upstream:**
    - Requires `OutlandHaven.UIToolkit.UIView` (Base Class).
    - Requires `OutlandHaven.UIToolkit.UIEventsSO` (Event channel for global UI screen transitions).
    - Requires `OutlandHaven.UIToolkit.ScreenType` (Enum for screen identification).
  - **Downstream:**
    - Inherited by concrete screen controllers (e.g., `HUDView`, `MageView`).

- **Data Schema:**
  - `UIEvents` (protected UIEventsSO): Holds the reference to broadcast screen state changes.

- **Side Effects & Lifecycle:**
  - **Lifecycle:** Derives from manual `UIView` lifecycle.
  - **Side Effects:** Triggers global `OnScreenOpen` event when shown, effectively notifying `UIManager` and other listeners of screen context changes.
