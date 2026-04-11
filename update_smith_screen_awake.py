import re

file_path = "./Toris/Assets/Scripts/UIToolkit/UI/Controllers/SmithScreenController.cs"
with open(file_path, "r") as f:
    content = f.read()

awake_block = """        void Awake()
        {
            _uiManager = FindFirstObjectByType<UIManager>();
            _bridge = _playerHudBridge != null ? _playerHudBridge : FindFirstObjectByType<PlayerHUDBridge>();

            if(_shopManagerSO != null) _shopManagerSO.Initialize();
            if(_craftingManagerSO != null) _craftingManagerSO.Initialize();
            if(_salvageManagerSO != null) _salvageManagerSO.Initialize();
        }"""

content = re.sub(r'void Awake\(\)\s*\{[^\}]+\}\s*\}', awake_block + '\n', content, flags=re.MULTILINE | re.DOTALL)
# wait, the regex above matches too broadly, let's just replace it explicitly

# explicit replace
old_awake = """        void Awake()
        {
            _uiManager = FindFirstObjectByType<UIManager>();

            if(_shopManagerSO != null) _shopManagerSO.Initialize();
            if(_craftingManagerSO != null) _craftingManagerSO.Initialize();
            if(_salvageManagerSO != null) _salvageManagerSO.Initialize();
        }"""

content = content.replace(old_awake, awake_block)

with open(file_path, "w") as f:
    f.write(content)
