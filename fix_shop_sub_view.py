import re

file_path = "./Toris/Assets/Scripts/UIToolkit/UI/UIViews/ShopSubView.cs"
with open(file_path, "r") as f:
    content = f.read()

# Make sure we don't have NullReferenceExceptions if _playerHudBridge is null
content = content.replace("if (!_eventsBound && _uiInventoryEvents != null)\n            {\n                _playerHudBridge.OnGoldChanged += HandleGoldChanged;",
                          "if (!_eventsBound && _uiInventoryEvents != null)\n            {\n                if (_playerHudBridge != null) _playerHudBridge.OnGoldChanged += HandleGoldChanged;")

content = content.replace("if (_eventsBound && _uiInventoryEvents != null)\n            {\n                _playerHudBridge.OnGoldChanged -= HandleGoldChanged;",
                          "if (_eventsBound && _uiInventoryEvents != null)\n            {\n                if (_playerHudBridge != null) _playerHudBridge.OnGoldChanged -= HandleGoldChanged;")

with open(file_path, "w") as f:
    f.write(content)
