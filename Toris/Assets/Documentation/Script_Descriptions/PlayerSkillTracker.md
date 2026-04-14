Identifier: `PlayerSkillTracker`
Architectural Role: Core Logic / Data Model
Core Logic (Abstract/Virtual Methods, Public API):
- `AddSP(int amount)`: Increases available SP.
- `TryUnlockSkill(SkillData skill)`: Attempts to unlock a skill, checking SP cost and prerequisites. Returns true on success.
- `ArePrerequisitesMet(SkillData skill)`: Checks if all prerequisites for a skill are met.
- `HasSkill(string skillID)`: Checks if a skill ID exists in the unlocked list.
Dependency Graph (Upstream/Downstream):
- Upstream: `SkillManager` (invokes `TryUnlockSkill`).
- Downstream: `SkillData` (reads prerequisites and cost).
Data Schema:
- `_availableSP` (int)
- `_unlockedSkillIDs` (List<string>)
Side Effects & Lifecycle:
- Serialized class stored in `GameSessionSO` or similar save data wrapper.
