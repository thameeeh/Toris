Identifier: `OutlandHaven.UIToolkit.SkillMenuController`
Architectural Role: Input Controller / Presenter Logic
Core Logic (Abstract/Virtual Methods, Public API):
- `OnToggleSkills(InputAction.CallbackContext context)`: Evaluates input and dispatches `UIEventsSO.OnRequestOpen` with `ScreenType.Skills`.
Dependency Graph (Upstream/Downstream):
- Upstream: `InputSystem_Actions` (listens for raw hardware inputs).
- Downstream: `UIEventsSO` (dispatches intent to open skill screen).
Data Schema:
- None
Side Effects & Lifecycle:
- Instantiates and enables `InputSystem_Actions` in `OnEnable()`, disabling and destroying it in `OnDisable()`.
