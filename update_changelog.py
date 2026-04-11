import os
import re
import datetime

file_path = "./Toris/Assets/Documentation/Changelog/CHANGELOG.md"
with open(file_path, "r") as f:
    content = f.read()

date_str = datetime.datetime.now().strftime("%Y-%m-%d")

new_entry = f"""## [Current] - Refactored UI Currency Access to Single Source of Truth
* Replaced `PlayerProgressionAnchorSO` with `PlayerHUDBridge` in `ShopSubView` and related controllers (`SmithScreenController`, `MageScreenController`).
* UI views now strictly observe `PlayerHUDBridge.OnGoldChanged` instead of global event channels for currency updates.
* Removed redundant `OnCurrencyChanged` event from `UIInventoryEventsSO` to prevent race conditions.
* Updated `ShopManagerSO`, `SalvageManagerSO`, and `CraftingManagerSO` to not invoke `OnCurrencyChanged`.

"""

content = content.replace("## [Current/Recent] - ", new_entry + "## [Previous] - ")
# actually there might be a different structure, let's just insert after the first ## [Current/Recent]
# Wait, let's see what CHANGELOG looks like first
