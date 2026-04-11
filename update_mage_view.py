import re

file_path = "./Toris/Assets/Scripts/UIToolkit/UI/UIViews/MageView.cs"
with open(file_path, "r") as f:
    content = f.read()

# Add _playerHudBridge field
content = content.replace("private ShopManagerSO _shopManager;",
                          "private ShopManagerSO _shopManager;\n        private PlayerHUDBridge _playerHudBridge;")

# Update constructor
content = content.replace("public MageView(VisualElement topElement, VisualTreeAsset slotTemplate, VisualTreeAsset shopTemplate, UIEventsSO uiEvents, UIInventoryEventsSO uiInventoryEvents, GameSessionSO gameSession, ShopManagerSO shopManager)",
                          "public MageView(VisualElement topElement, VisualTreeAsset slotTemplate, VisualTreeAsset shopTemplate, UIEventsSO uiEvents, UIInventoryEventsSO uiInventoryEvents, GameSessionSO gameSession, ShopManagerSO shopManager, PlayerHUDBridge playerHudBridge)")

content = content.replace("_shopManager = shopManager;", "_shopManager = shopManager;\n            _playerHudBridge = playerHudBridge;")

# Update ShopSubView instantiation
content = content.replace("_shopSubView = new ShopSubView(shopInstance, _slotTemplate, _uiInventoryEvents, _gameSession);",
                          "_shopSubView = new ShopSubView(shopInstance, _slotTemplate, _uiInventoryEvents, _gameSession, _playerHudBridge);")

with open(file_path, "w") as f:
    f.write(content)
