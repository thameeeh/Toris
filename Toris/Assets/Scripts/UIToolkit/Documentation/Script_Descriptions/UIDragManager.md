Identifier: OutlandHaven.UIToolkit.UIDragManager : MonoBehaviour

Architectural Role: Singleton Manager / UI Interaction Controller

Core Logic (The 'Contract'):
- Abstract/Virtual Methods: None.
- Public API:
  - `StartDrag(Sprite sprite, Vector2 position, Vector2 size)`: Initializes the visual ghost icon for dragging.
  - `UpdateDrag(Vector2 position)`: Updates the position of the ghost icon to match the pointer.
  - `StopDrag()`: Hides the ghost icon.

Dependency Graph (Crucial for Scaling):
- Upstream: Requires a `UIDocument` (preferably the global UI root).
- Downstream: Singleton `Instance` is accessed by draggable UI elements (like `InventorySlotView`) to orchestrate drag visual feedback.

Data Schema:
- UIDocument _uiDocument -> Reference to the root UI container.
- VisualElement _dragLayer -> Absolute-positioned, full-screen layer to house the drag ghost.
- VisualElement _ghostIcon -> Reusable element representing the dragged item.
- static UIDragManager Instance -> Global accessor.

Side Effects & Lifecycle:
- Awake: Initializes Singleton pattern (`Destroy(gameObject)` if duplicate). Auto-finds `UIDocument` if missing. Calls `InitializeDragLayer`.
- OnEnable: Failsafe initialization of `InitializeDragLayer` if not already set.
- InitializeDragLayer: Dynamically creates and injects a new `VisualElement` (`Drag_Layer`) directly into the `UIDocument` root. Sets its `pickingMode` to `Ignore` to prevent raycast blocking. Creates and adds the child `Ghost_Icon`.
- Directly manipulates inline styles (`left`, `top`, `display`, `backgroundImage`) of the `_ghostIcon` every frame a drag updates.