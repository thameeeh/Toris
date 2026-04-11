import re

file_path = "./Toris/Assets/Scripts/UIToolkit/UI/UIViews/ShopSubView.cs"
with open(file_path, "r") as f:
    content = f.read()

# Replace PlayerAnchor field with _playerHudBridge
content = content.replace("public PlayerProgressionAnchorSO PlayerAnchor;", "private PlayerHUDBridge _playerHudBridge;")

# Update Constructor
content = content.replace("public ShopSubView(VisualElement topElement, VisualTreeAsset slotTemplate, UIInventoryEventsSO uiInventoryEvents, GameSessionSO gameSession)",
                          "public ShopSubView(VisualElement topElement, VisualTreeAsset slotTemplate, UIInventoryEventsSO uiInventoryEvents, GameSessionSO gameSession, PlayerHUDBridge playerHudBridge)")

content = content.replace("_gameSession = gameSession;", "_gameSession = gameSession;\n            _playerHudBridge = playerHudBridge;")

# Update Setup()
content = content.replace("if (_gameSession != null && PlayerAnchor != null)\n            {\n                UpdateGoldAmount(PlayerAnchor.Instance.CurrentGold);\n            }",
                          "if (_gameSession != null && _playerHudBridge != null)\n            {\n                UpdateGoldAmount(_playerHudBridge.CurrentGold);\n            }")

# Replace _uiInventoryEvents.OnCurrencyChanged with _playerHudBridge.OnGoldChanged
content = content.replace("_uiInventoryEvents.OnCurrencyChanged += UpdateGoldAmount;",
                          "_playerHudBridge.OnGoldChanged += HandleGoldChanged;")
content = content.replace("_uiInventoryEvents.OnCurrencyChanged -= UpdateGoldAmount;",
                          "_playerHudBridge.OnGoldChanged -= HandleGoldChanged;")


# add HandleGoldChanged method right above UpdateGoldAmount
gold_handler = """        private void HandleGoldChanged(int currentGold, int delta)
        {
            UpdateGoldAmount(currentGold);
        }

        private void UpdateGoldAmount(int amount)"""

content = content.replace("private void UpdateGoldAmount(int amount)", gold_handler)

with open(file_path, "w") as f:
    f.write(content)
