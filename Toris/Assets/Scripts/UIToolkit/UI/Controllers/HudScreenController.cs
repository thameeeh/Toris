using UnityEngine;
using UnityEngine.UIElements;
using OutlandHaven.Inventory;

namespace OutlandHaven.UIToolkit
{
    public class HudScreenController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private VisualTreeAsset _hudMainTemplate; // <--- Drag HUD.uxml here
        [SerializeField] private VisualTreeAsset _buttonTemplate;
        [SerializeField] private VisualTreeAsset _slotTemplate;
        [SerializeField] private UIInventoryEventsSO _uiInventoryEvents;
        [SerializeField] private GameSessionSO _gameSession;
        [SerializeField] private UIEventsSO _uiEvents;
        [SerializeField] private InventoryManager _potionInventory;

        private HUDView _view;
        private UIManager _uiManager;
        private PlayerHUDBridge _playerHudBridge;

        void Awake()
        {
            _uiManager = FindFirstObjectByType<UIManager>();
            _playerHudBridge = FindFirstObjectByType<PlayerHUDBridge>();
        }

        private void OnEnable()
        {
            if (_hudMainTemplate == null)
                return;
        }

        private void Start()
        {
            if (_hudMainTemplate == null) return;

            // 1. Instantiate the UI from the asset
            TemplateContainer hudInstance = _hudMainTemplate.Instantiate();

            if (_playerHudBridge == null)
            {
                Debug.LogWarning($"[UI/Inventory] <b><color=yellow>HudScreenController</color></b> must be on active <b><color=green>GameObject</color></b>");
            }

            // 2. Pass the INSTANCE to the View
            _view = new HUDView(hudInstance, _playerHudBridge, _uiEvents, _uiInventoryEvents, _buttonTemplate, _slotTemplate);
            _view.Initialize();
            _view.Setup(_potionInventory);

            // 3. Register to the HUD Zone
            _uiManager.RegisterView(_view, ScreenZone.HUD);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_hudMainTemplate == null)
                Debug.LogWarning($"[UI/Inventory] {name}: HUD Main Template is missing! <color=yellow>HudScreenController must be on active GameObject</color>", this);
            if (_buttonTemplate == null)
                Debug.LogWarning($"[UI/Inventory] <color=red>{name}</color> missing Button Template", this);
            if (_slotTemplate == null)
                Debug.LogWarning($"[UI/Inventory] <color=red>{name}</color> missing Slot Template", this);
            if (_uiInventoryEvents == null)
                Debug.LogError($"<b><color=red>[HUD]</color></b> missing <b>UIInventoryEventsSO</b> on GameObject: <b>{name}</b>", this);
            if (_uiEvents == null)
                Debug.LogWarning($"[UI/Inventory] <color=red>{name}</color> missing UI Events SO", this);
            if (_gameSession == null)
                Debug.LogWarning($"[UI/Inventory] <color=red>{name}</color> missing Game Session SO", this);
        }
#endif
    }
}