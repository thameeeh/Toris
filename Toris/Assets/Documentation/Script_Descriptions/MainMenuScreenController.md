Identifier: OutlandHaven.UIToolkit.MainMenuScreenController : MonoBehaviour

Architectural Role: Component Logic

Core Logic:
- Abstract/Virtual Methods: None
- Public API:
  - QuitGame(): Stops Unity Editor play mode or quits the built application runtime.

Dependency Graph:
- Upstream: Depends on UIDocument (UI Toolkit element mapping), GameSessionSO, UnityEngine.SceneManagement.SceneManager.
- Downstream: Handled natively by UI Toolkit click events.

Data Schema:
- GameSessionSO gameSession -> Session context data for save/load operations.
- string VillageSceneName -> Target scene name to load when starting a new game (default: "MainArea").
- UIDocument _doc -> Reference to the attached main menu UI document.

Side Effects & Lifecycle:
- OnEnable: Queries UIDocument root for buttons (btn-start-game, btn-quit-game) and subscribes to their clicked events.
- OnDisable: Unsubscribes from button clicked events to prevent memory leaks.
- Event Callbacks: Triggering btn-start-game calls SceneManager.LoadScene to transition game context.
