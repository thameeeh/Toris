import re

file_path = "./Toris/Assets/Scripts/UIToolkit/UI/Controllers/HudScreenController.cs"
with open(file_path, "r") as f:
    content = f.read()

content = content.replace("[SerializeField] private PlayerProgressionAnchorSO _playerAnchor;\n", "")
content = content.replace("        [SerializeField] private PlayerStatsAnchorSO _playerStatsAnchor;\n", "")
content = content.replace("[SerializeField] private PlayerStatsAnchorSO _playerStatsAnchor;", "")

with open(file_path, "w") as f:
    f.write(content)
