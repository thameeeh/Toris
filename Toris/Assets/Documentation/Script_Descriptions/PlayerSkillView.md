Identifier: `OutlandHaven.Skills.PlayerSkillView`
Architectural Role: UI View / Component Logic
Core Logic (Abstract/Virtual Methods, Public API):
- Inherits from `GameView`, implements `IDisposable`.
- `SetVisualElements()`: Queries UI elements for info panel and unlock button.
- `RegisterButtonCallbacks()`: Binds skill nodes (`node_double_jump`, `node_dash`) to specific `SkillData` IDs.
- `Setup(object payload)`: Resets info panel and unpacks `SkillsPayload` data.
- `Dispose()`: Unregisters event callbacks to prevent memory leaks.
Dependency Graph (Upstream/Downstream):
- Upstream: `SkillsScreenController` (instantiates and provides `SkillsPayload` and `SkillData[]`).
- Downstream: `SkillData` (consumes data to populate UI).
Data Schema:
- `SkillsPayload`: Struct containing stats (`Strength`, `Agility`, `Intelligence`) and XP percentages.
Side Effects & Lifecycle:
- Unregisters events explicitly during `Dispose()`.
