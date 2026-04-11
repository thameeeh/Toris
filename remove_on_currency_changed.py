import re

file_path = "./Toris/Assets/Scripts/UIToolkit/UI/Events/UIInventoryEventsSO.cs"
with open(file_path, "r") as f:
    content = f.read()

content = content.replace("public UnityAction<int> OnCurrencyChanged;\n", "")

with open(file_path, "w") as f:
    f.write(content)
