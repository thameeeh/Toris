Identifier: `OutlandHaven.UIToolkit.SkillsScreenController`
Architectural Role: UI Screen Controller / Component Logic
Core Logic (Abstract/Virtual Methods, Public API):
- `Start()`: Instantiates `SkillScreen.uxml` into a `TemplateContainer`, initializes `PlayerSkillView`, and registers it to `UIManager` (`ScreenZone.FullScreen`).
- `HandleScreenOpen(ScreenType type)`: Listens for `ScreenType.Skills` to invoke `UpdateViewData()`.
- `UpdateViewData()`: Constructs a `SkillsPayload` and pushes it to `PlayerSkillView.Setup()`.
Dependency Graph (Upstream/Downstream):
- Upstream: `UIManager`, `UIEventsSO` (listens for screen open requests).
- Downstream: `PlayerSkillView` (instantiates and injects data), `SkillData[]`.
Data Schema:
- None
Side Effects & Lifecycle:
- Binds to `UIEventsSO.OnScreenOpen` during `OnEnable`, unbinds in `OnDisable`.
- Validates serialized fields (`_skillsMainTemplate`, `_uiEvents`, `_skillDatabase`) during `OnValidate()`.
