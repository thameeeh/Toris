import re

file_path = "./Toris/Assets/Documentation/Changelog/CHANGELOG.md"
with open(file_path, "r") as f:
    content = f.read()

new_entry = """## [Current/Recent] - Refactored UI Currency Access
* Replaced `PlayerProgressionAnchorSO` with `PlayerHUDBridge` in `ShopSubView` and related controllers (`SmithScreenController`, `MageScreenController`).
* UI views now strictly observe `PlayerHUDBridge.OnGoldChanged` instead of global event channels for currency updates.
* Removed redundant `OnCurrencyChanged` event from `UIInventoryEventsSO` to prevent race conditions.
* Updated `ShopManagerSO`, `SalvageManagerSO`, and `CraftingManagerSO` to not invoke `OnCurrencyChanged`.
* Removed unused `PlayerStatsAnchorSO` from `HudScreenController`.

"""

content = content.replace("## [Current/Recent] - Assign Skill Screen to Input Key", new_entry + "## [Previous] - Assign Skill Screen to Input Key")

with open(file_path, "w") as f:
    f.write(content)
