Identifier: OutlandHaven.UIToolkit.WorldContainer : MonoBehaviour

Architectural Role: Component Logic (World Interaction)

Core Logic:
* Abstract/Virtual Methods: None
* Public API: None (All methods are private lifecycle or interaction handlers).

Dependency Graph:
* Upstream: Requires `InventoryManager`, `UIEventsSO`, `Collider2D` (attached to object), "Player" tag on interacting object.
* Downstream: Interacts with UI Managers (via `UIEventsSO`).

Data Schema:
* `InventoryManager _containerData`: Serialized reference to the container's inventory data component.
* `UIEventsSO _uiEvents`: Serialized reference to the global UI event channel.
* `KeyCode _interactKey`: Serialized keybind for interaction (Default: F).
* `bool _playerInRange`: Tracks if the player is within the interaction trigger.

Side Effects & Lifecycle:
* Initialization: Uses `Awake` to self-assign `_containerData` if null. Uses `OnValidate` to check for missing dependencies in the Editor.
* Allocations: No significant per-frame allocations.
* Lifecycle:
  * `Update`: Listens for `_interactKey` input when `_playerInRange` is true. Calls `OpenContainer()` which fires `OnRequestOpen`.
  * `OnTriggerEnter2D`: Sets `_playerInRange` true if the colliding object has the "Player" tag.
  * `OnTriggerExit2D`: Sets `_playerInRange` false and fires `OnRequestClose` if the colliding object has the "Player" tag.
