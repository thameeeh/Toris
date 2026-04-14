Identifier: `SkillData`
Architectural Role: Data Container / ScriptableObject Blueprint
Core Logic (Abstract/Virtual Methods, Public API):
- Acts as a serialized container for individual skill definitions.
Dependency Graph (Upstream/Downstream):
- Downstream: Used by `PlayerSkillView` and `SkillsScreenController`.
Data Schema:
- `skillID` (string)
- `skillName` (string)
- `description` (string)
- `costSP` (int)
- `prerequisites` (SkillData[])
Side Effects & Lifecycle:
- Static configuration object.
