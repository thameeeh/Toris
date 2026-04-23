Identifier: OutlandHaven.UIToolkit.ScreenType / ScreenZone / UIEvents : Enum / Static Class

Architectural Role: Data Container / Decoupled Event

Core Logic (The 'Contract'):
- Abstract/Virtual Methods: None
- Public API: None (Data only, static event delegate `OnScreenOpen`)

Dependency Graph (Crucial for Scaling):
- Upstream: None
- Downstream: Referenced by UIManager, UIEventsSO, and system managers managing UI screen logic and exclusivity.

Data Schema:
- ScreenType (Enum) -> Logical window identifiers (None, HUD, Inventory, CharacterSheet, PauseMenu, Smith, Mage).
- ScreenZone (Enum) -> UI layout regions for window exclusivity (HUD, Left, Right, Modal).
- UIEvents.OnScreenOpen (Action<ScreenType>) -> Static event broadcast when a screen finishes opening.

Side Effects & Lifecycle:
- Passive enums and static delegate. No instantiation or MonoBehaviour lifecycle methods.
