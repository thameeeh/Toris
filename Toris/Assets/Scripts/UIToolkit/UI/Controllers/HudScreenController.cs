using UnityEngine;
using UnityEngine.UIElements;

namespace OutlandHaven.UIToolkit
{
    public class HudScreenController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private VisualTreeAsset _hudMainTemplate; // <--- Drag HUD.uxml here
        [SerializeField] private VisualTreeAsset _buttonTemplate;
        [SerializeField] private GameSessionSO _gameSession;
        [SerializeField] private UIEventsSO _uiEvents;

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
            {
                Debug.LogError("HudScreenController: HUD Main Template is missing! <color=yellow>HudScreenController must be on active GameObject</color>");
                return;
            }
        }

        private void Start()
        {
            if (_hudMainTemplate == null) return;

            // 1. Instantiate the UI from the asset
            TemplateContainer hudInstance = _hudMainTemplate.Instantiate();

            if (_playerHudBridge == null)
            {
                Debug.LogWarning("<b><color=yellow>HudScreenController</color></b> must be on active <b><color=green>GameObject</color></b>");
            }

            // 2. Pass the INSTANCE to the View
            _view = new HUDView(hudInstance, _playerHudBridge, _uiEvents, _buttonTemplate);
            _view.Initialize();

            // 3. Register to the HUD Zone
            _uiManager.RegisterView(_view, ScreenZone.HUD);
        }

        private void OnValidate()
        {
            if (_uiEvents == null)
            {
                Debug.LogError($" <color=red>{name}</color> missing UI Events SO", this);
            }
            if (_buttonTemplate == null)
            {
                Debug.LogError($" <color=red>{name}</color> missing Button Template", this);
            }
        }
    }
}