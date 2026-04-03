Identifier: OutlandHaven.UIToolkit.HudScreenController : MonoBehaviour

Architectural Role: Component Logic

Core Logic:
- Abstract/Virtual Methods: None
- Public API: None

Dependency Graph:
- Upstream: Depends on UIManager, PlayerHUDBridge, VisualTreeAsset (UI templates), GameSessionSO, UIEventsSO, PlayerProgressionAnchorSO, PlayerStatsAnchorSO, HUDView.
- Downstream: None.

Data Schema:
- VisualTreeAsset _hudMainTemplate -> Main HUD UI markup template.
- VisualTreeAsset _buttonTemplate -> Reusable UI button template.
- GameSessionSO _gameSession -> Reference to global game session state.
- UIEventsSO _uiEvents -> Global UI event channel.
- PlayerProgressionAnchorSO _playerAnchor -> Player progression data anchor.
- PlayerStatsAnchorSO _playerStatsAnchor -> Player stats data anchor.

Side Effects & Lifecycle:
- Awake: Queries the scene for UIManager and PlayerHUDBridge singletons.
- OnEnable/OnValidate: Editor and runtime validation of required references.
- Start: Instantiates _hudMainTemplate, allocates and initializes a new HUDView instance, and registers the view to UIManager on ScreenZone.HUD.
