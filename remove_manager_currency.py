import re

files = [
    "./Toris/Assets/Scripts/UIToolkit/ScriptableObjects/ShopManagerSO.cs",
    "./Toris/Assets/Scripts/UIToolkit/ScriptableObjects/SalvageManagerSO.cs",
    "./Toris/Assets/Scripts/UIToolkit/ScriptableObjects/CraftingManagerSO.cs"
]

for file_path in files:
    with open(file_path, "r") as f:
        content = f.read()

    # Find and remove any lines containing OnCurrencyChanged
    lines = content.split('\n')
    new_lines = [line for line in lines if "OnCurrencyChanged" not in line]

    with open(file_path, "w") as f:
        f.write('\n'.join(new_lines))

print("Done")
