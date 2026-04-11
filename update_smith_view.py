import re

file_path = "./Toris/Assets/Scripts/UIToolkit/UI/UIViews/SmithView.cs"
with open(file_path, "r") as f:
    content = f.read()

content = content.replace("private PlayerProgressionAnchorSO _playerAnchor;", "private PlayerHUDBridge _playerHudBridge;")

content = content.replace("public SmithView(VisualElement topElement, VisualTreeAsset slotTemplate, VisualTreeAsset shopTemplate, VisualTreeAsset forgeTemplate, VisualTreeAsset salvageTemplate, UIEventsSO uiEvents, UIInventoryEventsSO uiInventoryEvents, GameSessionSO gameSession, PlayerProgressionAnchorSO playerAnchor, CraftingManagerSO craftingManager, SalvageManagerSO salvageManager)",
                          "public SmithView(VisualElement topElement, VisualTreeAsset slotTemplate, VisualTreeAsset shopTemplate, VisualTreeAsset forgeTemplate, VisualTreeAsset salvageTemplate, UIEventsSO uiEvents, UIInventoryEventsSO uiInventoryEvents, GameSessionSO gameSession, PlayerHUDBridge playerHudBridge, CraftingManagerSO craftingManager, SalvageManagerSO salvageManager)")

content = content.replace("_playerAnchor = playerAnchor;", "_playerHudBridge = playerHudBridge;")

# It seems SmithView was modified previously, but I need to make sure the instantiation of ShopSubView is updated properly. Let's check how it initializes it:
content = content.replace("_shopSubView = new ShopSubView(shopInstance, _slotTemplate, _uiInventoryEvents, _gameSession);\n                    _shopSubView.PlayerAnchor = _playerAnchor;",
                          "_shopSubView = new ShopSubView(shopInstance, _slotTemplate, _uiInventoryEvents, _gameSession, _playerHudBridge);")

# Also need to check if there are other occurrences.
with open(file_path, "w") as f:
    f.write(content)
