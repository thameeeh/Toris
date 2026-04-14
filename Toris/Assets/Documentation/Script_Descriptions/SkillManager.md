Identifier: `OutlandHaven.Skills.SkillManager`
Architectural Role: Manager / Presenter Logic
Core Logic (Abstract/Virtual Methods, Public API):
- `HandleUnlockRequest(SkillData skill)`: Attempts to unlock a skill using `GameSessionSO.PlayerSkills.TryUnlockSkill(skill)`. If successful, broadcasts `OnSkillUnlocked` and `OnSPUpdated`.
Dependency Graph (Upstream/Downstream):
- Upstream: `UISkillEventsSO` (listens to `OnRequestUnlock`).
- Downstream: `GameSessionSO` (modifies `PlayerSkillTracker`), `UISkillEventsSO` (invokes update events).
Data Schema:
- None
Side Effects & Lifecycle:
- Binds to `UISkillEventsSO.OnRequestUnlock` during `OnEnable`, unbinds in `OnDisable`.
