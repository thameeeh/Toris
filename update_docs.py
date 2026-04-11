import os
import re

def process_file(filepath):
    if not os.path.exists(filepath):
        print(f"Skipping {filepath} (does not exist)")
        return
    with open(filepath, "r") as f:
        content = f.read()

    # Generic Replacements
    content = content.replace("PlayerProgressionAnchorSO", "PlayerHUDBridge")

    lines = content.split('\n')
    new_lines = []
    for line in lines:
        if "OnCurrencyChanged" in line:
            # Try to just remove OnCurrencyChanged from the list if there are multiple events
            line = re.sub(r',\s*OnCurrencyChanged', '', line)
            line = re.sub(r'OnCurrencyChanged,\s*', '', line)
            line = re.sub(r'OnCurrencyChanged', '', line)

            # If the line became empty or just a bullet point, we might want to drop it entirely
            if line.strip() in ['*', '-', '`()', '``', '']:
                continue

            # specific cleanup for descriptions
            if line.strip() == "- UnityAction<int>  -> Broadcaster for currency changes.":
                continue
            if line.strip() == "*   **(int)**: Fired when player gold increases/decreases. Currency displays listen to this.":
                continue
            if "Fired when gold values update" in line:
                continue
            if line.strip() == "*   `(int newAmount)`":
                continue
            if "and " in line and line.strip().endswith("and"):
                line = line.replace(" and", "")

        # fix some of the grammar since we replaced PlayerProgressionAnchorSO with PlayerHUDBridge
        line = line.replace("PlayerHUDBridge PlayerAnchor", "PlayerHUDBridge _playerHudBridge")

        new_lines.append(line)

    with open(filepath, "w") as f:
        f.write('\n'.join(new_lines))

docs = [
    "./Toris/Assets/Editor/Documentation/Shop_Architecture_Documentation.md",
    "./Toris/Assets/Editor/Documentation/Event_Architecture_Documentation.md",
    "./Toris/Assets/Editor/Documentation/script dependency documentation.md",
    "./Toris/Assets/Documentation/Shop_Architecture_Documentation.md",
    "./Toris/Assets/Documentation/Event_Architecture_Documentation.md",
    "./Toris/Assets/Documentation/script dependency documentation.md",
    "./Toris/Assets/Documentation/Inventory_Event_System_Documentation.md",
    "./Toris/Assets/Documentation/Script_Descriptions/UIInventoryEventsSO.md",
    "./Toris/Assets/Documentation/Script_Descriptions/CraftingManagerSO.md",
    "./Toris/Assets/Documentation/Script_Descriptions/SalvageManagerSO.md",
    "./Toris/Assets/Documentation/Script_Descriptions/ShopSubView.md",
    "./Toris/Assets/Documentation/Script_Descriptions/ShopManagerSO.md",
    "./Toris/Assets/Documentation/Script_Descriptions/SmithScreenController.md",
    "./Toris/Assets/Documentation/Script_Descriptions/HudScreenController.md",
    "./Toris/Assets/Documentation/Script_Descriptions/SmithView.md",
    "./Toris/Assets/Documentation/Script_Descriptions/MageView.md"
]

for doc in docs:
    process_file(doc)
