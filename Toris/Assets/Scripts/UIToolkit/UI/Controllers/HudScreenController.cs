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
        [SerializeField] private VisualTreeAsset _abilitySlotTemplate;
        [SerializeField] private UIInventoryEventsSO _uiInventoryEvents;
        [SerializeField] private OutlandHaven.Skills.UISkillEventsSO _uiSkillEvents;
        [SerializeField] private GameSessionSO _gameSession;
        [SerializeField] private UIEventsSO _uiEvents;

        private HUDView _view;
        private UIManager _uiManager;
        private InventoryManager _potionInventory;
        private PlayerAbilityController _abilityController;
        private readonly FpsCounterPresenter _fpsCounter = new FpsCounterPresenter();
        private bool _showFps;

        void Awake()
        {
            _uiManager = FindFirstObjectByType<UIManager>();
            _abilityController = FindFirstObjectByType<PlayerAbilityController>();
            if (_gameSession == null) _gameSession = GameSessionSO.LoadDefault();
        }

        private void OnEnable()
        {
            FpsDisplaySettings.OnShowFpsChanged += HandleShowFpsChanged;
            HandleShowFpsChanged(FpsDisplaySettings.ShowFps);
        }

        private void OnDisable()
        {
            FpsDisplaySettings.OnShowFpsChanged -= HandleShowFpsChanged;
        }

        private void Update()
        {
            if (!_showFps || _view == null)
            {
                return;
            }

            if (_fpsCounter.TryTick(Time.unscaledDeltaTime, out string fpsText))
            {
                _view.SetFpsText(fpsText);
            }
        }

        private void Start()
        {
            if (_hudMainTemplate == null) return;
            if (_gameSession == null) _gameSession = GameSessionSO.LoadDefault();

            _potionInventory = _gameSession.PlayerPotionInventory;

            // 1. Instantiate the UI from the asset
            TemplateContainer hudInstance = _hudMainTemplate.Instantiate();

            if (_gameSession.PlayerHUD == null)
            {
                Debug.LogWarning($"[UI/Inventory] <b><color=yellow>HudScreenController</color></b>: PlayerHUD reference in GameSession is null! Ensure PlayerHUDBridge is active in the scene.");
            }

            // 2. Pass the INSTANCE to the View
            _view = new HUDView(hudInstance, _gameSession.PlayerHUD, _gameSession.PlayerSkills, _uiEvents, _uiInventoryEvents, _uiSkillEvents, _buttonTemplate, _slotTemplate, _abilitySlotTemplate);
            _view.Initialize();
            
            // Setup with both inventories/controllers
            _view.Setup((_potionInventory, _abilityController));

            // 3. Register to the HUD Zone
            _uiManager.RegisterView(_view, ScreenZone.HUD);
            HandleShowFpsChanged(FpsDisplaySettings.ShowFps);
        }

        private void HandleShowFpsChanged(bool showFps)
        {
            _showFps = showFps;
            _fpsCounter.Reset();
            // Settings FPS hook: HUD samples frame rate only while the user-facing toggle is enabled.
            _view?.SetFpsVisible(showFps);
            if (showFps)
            {
                _view?.SetFpsText(_fpsCounter.CurrentText);
            }
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
            if (_abilitySlotTemplate == null)
                Debug.LogWarning($"[UI/Inventory] <color=red>{name}</color> missing Ability Slot Template", this);
            if (_uiInventoryEvents == null)
                Debug.LogError($"<b><color=red>[HUD]</color></b> missing <b>UIInventoryEventsSO</b> on GameObject: <b>{name}</b>", this);
            if (_uiSkillEvents == null)
                Debug.LogError($"<b><color=red>[HUD]</color></b> missing <b>UISkillEventsSO</b> on GameObject: <b>{name}</b>", this);
            if (_uiEvents == null)
                Debug.LogWarning($"[UI/Inventory] <color=red>{name}</color> missing UI Events SO", this);
            if (_gameSession == null)
                Debug.LogWarning($"[UI/Inventory] <color=red>{name}</color> missing Game Session SO", this);
        }
#endif
    }
}
