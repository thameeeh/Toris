- **Identifier:** `OutlandHaven.UIToolkit.UIView : IDisposable`
- **Architectural Role:** Abstract Blueprint / Base Class for UI Toolkit functional units (screens, sub-views, or components).

- **Core Logic (The 'Contract'):**
  - **Abstract/Virtual Methods:**
    - `Initialize()`: Handles initial state (`Hide` if `m_HideOnAwake`), calls element binding and callback registration. Expected to be extended by children.
    - `Setup(object payload = null)`: Hook for data injection/configuration prior to display.
    - `SetVisualElements()`: Hook for binding C# fields to UXML elements (e.g., via `Q<T>()`).
    - `RegisterButtonCallbacks()`: Hook for subscribing to UI Toolkit interaction events.
    - `Show()`: Toggles visibility. Expected to override if custom animation/logic is needed.
    - `Hide()`: Toggles visibility. Expected to override if custom animation/logic is needed.
    - `Dispose()`: Expected behavior for children is to unregister all UI events to prevent memory leaks.
  - **Public API:**
    - `UIView(VisualElement topElement)`: Constructor; requires and binds the root UXML element.
    - `Initialize()`: Triggers internal setup sequence.
    - `Setup(object payload = null)`: Receives optional dynamic context data.
    - `Show()`: Sets root display style to `Flex`.
    - `Hide()`: Sets root display style to `None`.
    - `Dispose()`: Cleans up resources/event listeners.

- **Dependency Graph (Crucial for Scaling):**
  - **Upstream:**
    - Requires `UnityEngine.UIElements.VisualElement`.
    - Requires `System.IDisposable`.
  - **Downstream:**
    - Inherited by specific UI view implementations (e.g., `ShopSubView`, `PlayerInventoryView`).
    - Handled and instantiated by UI Presenters/Controllers or parent Views.

- **Data Schema:**
  - `m_HideOnAwake` (protected bool): Determines if the view initializes hidden. Default `true`.
  - `m_IsOverlay` (protected bool): Flag indicating if UI reveals underlying UIs (transparent).
  - `m_TopElement` (protected VisualElement): The root UI Toolkit node for this view.
  - `Root` (public VisualElement getter): Read-only access to `m_TopElement`.
  - `IsTransparent` (public bool getter): Read-only access to `m_IsOverlay`.
  - `IsHidden` (public bool getter): Evaluates dynamically if `m_TopElement.style.display` equals `DisplayStyle.None`.

- **Side Effects & Lifecycle:**
  - **Lifecycle:** Pure C# object (not a `MonoBehaviour`). Uses manual initialization and cleanup managed by an external UI Controller. Does not use Unity update loops (`Update`, `FixedUpdate`).
  - **Side Effects:** Directly mutates `style.display` on the bound `VisualElement`.
  - **Memory:** Relies on manual `Dispose()` calls from parent controllers to unsubscribe events; failure to do so in child classes will cause managed memory leaks.
