Identifier: `OutlandHaven.Skills.UISkillEventsSO`
Architectural Role: Event Bus / Decoupling Layer
Core Logic (Abstract/Virtual Methods, Public API):
- Provides `UnityAction` events for system-to-UI (`OnSPUpdated`, `OnSkillUnlocked`) and UI-to-system (`OnRequestUnlock`) communication.
Dependency Graph (Upstream/Downstream):
- Upstream: `SkillManager` (listens to `OnRequestUnlock`, invokes `OnSPUpdated`, `OnSkillUnlocked`), `PlayerSkillView` (listens to `OnSkillUnlocked`, invokes `OnRequestUnlock`).
Data Schema:
- None
Side Effects & Lifecycle:
- Static event bus instantiated as a ScriptableObject.
