import re

file_path = "./Toris/Assets/Scripts/UIToolkit/UI/Controllers/MageScreenController.cs"
with open(file_path, "r") as f:
    content = f.read()

# Add _playerHudBridge serialization
content = content.replace("[SerializeField] private GameSessionSO _gameSession;",
                          "[SerializeField] private GameSessionSO _gameSession;\n        [SerializeField] private PlayerHUDBridge _playerHudBridge;")

# Update UIManager and add bridge ref
content = content.replace("private UIManager _uiManager;",
                          "private UIManager _uiManager;\n        private PlayerHUDBridge _bridge;")

# Update Awake
old_awake = """        void Awake()
        {
            _uiManager = FindFirstObjectByType<UIManager>();

            if(_shopManagerSO != null) _shopManagerSO.Initialize();
        }"""
new_awake = """        void Awake()
        {
            _uiManager = FindFirstObjectByType<UIManager>();
            _bridge = _playerHudBridge != null ? _playerHudBridge : FindFirstObjectByType<PlayerHUDBridge>();

            if(_shopManagerSO != null) _shopManagerSO.Initialize();
        }"""
content = content.replace(old_awake, new_awake)

# Update _view initialization
content = content.replace("_view = new MageView(mageInstance, _slotTemplate, _shopTemplate, _uiEvents, _uiInventoryEvents, _gameSession, _shopManagerSO);",
                          "_view = new MageView(mageInstance, _slotTemplate, _shopTemplate, _uiEvents, _uiInventoryEvents, _gameSession, _shopManagerSO, _bridge);")

with open(file_path, "w") as f:
    f.write(content)
