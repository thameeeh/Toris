Identifier: OutlandHaven.UIToolkit.UIManager : MonoBehaviour

Architectural Role: Singleton Manager / Screen Visibility and Zone Arbitrator

Core Logic (The 'Contract'):
- Abstract/Virtual Methods: None
- Public API:
  - RegisterView(GameView view, ScreenZone zone): Maps a view to a zone and adds it to the UIDocument layout. Shows HUD by default.

Dependency Graph (Crucial for Scaling):
- Upstream: Depends on UIEventsSO (event channel), UIDocument (root element), GameView (registered elements), ScreenZone/ScreenType (Enums).
- Downstream: Subscribed to by UIEventsSO. Controllers (e.g., PlayerController) call RegisterView to inject views.

Data Schema:
- UIEventsSO _UIEvents -> Event bus for open/close window requests.
- bool showHudOnStart -> Configuration toggle.
- List<GameView> _allViews -> Tracks all managed UI screens.
- Dictionary<GameView, ScreenZone> _viewZones -> Maps views to their display regions (HUD, Left, Right) for exclusivity logic.

Side Effects & Lifecycle:
- Awake: Queries UIDocument for visual layout containers ("Layer_HUD", "Left_Zone", "Right_Zone"). Logs error if missing.
- OnEnable/OnDisable: Subscribes/unsubscribes to global UIEventsSO channels (OnRequestOpen, OnRequestClose, OnRequestCloseAll).
- OnValidate: Validates _UIEvents serialization.
- Window Management: Opening a view in a non-HUD zone automatically closes other views in that same zone. Opening Smith or Mage views triggers a side-effect opening of the Inventory view.
