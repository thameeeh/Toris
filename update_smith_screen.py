import re

file_path = "./Toris/Assets/Scripts/UIToolkit/UI/Controllers/SmithScreenController.cs"
with open(file_path, "r") as f:
    content = f.read()

content = content.replace("[SerializeField] private PlayerProgressionAnchorSO _playerAnchor;",
                          "[SerializeField] private PlayerHUDBridge _playerHudBridge;")

content = content.replace("private UIManager _uiManager;",
                          "private UIManager _uiManager;\n        private PlayerHUDBridge _bridge;\n")

# Awake logic update
awake_block = """        void Awake()
        {
            _uiManager = FindFirstObjectByType<UIManager>();
            _bridge = _playerHudBridge != null ? _playerHudBridge : FindFirstObjectByType<PlayerHUDBridge>();

            if(_shopManagerSO != null) _shopManagerSO.Initialize();
            if(_craftingManagerSO != null) _craftingManagerSO.Initialize();
            if(_salvageManagerSO != null) _salvageManagerSO.Initialize();
        }"""
content = re.sub(r'void Awake\(\)\s*\{[^\}]+\}\s*\}', awake_block + '\n', content, flags=re.MULTILINE | re.DOTALL)

# Let's use simpler regex or targeted replacement for awake
content = content.replace("_view = new SmithView(smithInstance, _slotTemplate, _shopTemplate, _forgeTemplate, _salvageTemplate, _uiEvents, _uiInventoryEvents, _gameSession, _playerAnchor, _craftingManagerSO, _salvageManagerSO);",
                          "_view = new SmithView(smithInstance, _slotTemplate, _shopTemplate, _forgeTemplate, _salvageTemplate, _uiEvents, _uiInventoryEvents, _gameSession, _bridge, _craftingManagerSO, _salvageManagerSO);")

with open(file_path, "w") as f:
    f.write(content)
