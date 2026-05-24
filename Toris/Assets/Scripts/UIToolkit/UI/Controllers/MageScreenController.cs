using OutlandHaven.UIToolkit;
using UnityEngine;
using UnityEngine.UIElements;
using OutlandHaven.Inventory;

namespace OutlandHaven.UIToolkit
{
    public class MageScreenController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private VisualTreeAsset _mageMainTemplate; // <--- Drag Mage.uxml here
        [SerializeField] private VisualTreeAsset _slotTemplate; // <--- DRAG Slot.uxml HERE
        [SerializeField] private VisualTreeAsset _shopTemplate; // <--- DRAG ShopSubView.uxml HERE
        [SerializeField] private UIEventsSO _uiEvents;
        [SerializeField] private UIInventoryEventsSO _uiInventoryEvents;
        [SerializeField] private GameSessionSO _gameSession;
        [SerializeField] private ShopManagerSO _shopManagerSO;

        private MageView _view;
        private UIManager _uiManager;

        void Awake()
        {
            _uiManager = FindFirstObjectByType<UIManager>();
            if (_gameSession == null) _gameSession = GameSessionSO.LoadDefault();

            if(_shopManagerSO != null) _shopManagerSO.Initialize();
        }
        private void OnEnable()
        {
            if (_uiEvents != null)
            {
                _uiEvents.OnRequestOpen += HandleRequestOpen;
                _uiEvents.OnScreenOpen += HandleScreenOpen;
            }

            if (_mageMainTemplate == null)
            {
                Debug.LogError("MageScreenController: Mage Main Template is missing!");
                return;
            }
            if (_slotTemplate == null)
            {
                Debug.LogError("MageScreenController: Slot Template is missing!");
                return;
            }
        }

        private void OnDisable()
        {
            if (_uiEvents != null)
            {
                _uiEvents.OnRequestOpen -= HandleRequestOpen;
                _uiEvents.OnScreenOpen -= HandleScreenOpen;
            }
        }

        private void HandleRequestOpen(ScreenType screenType, object payload)
        {
            if (screenType != ScreenType.Mage) return;

            // 1. The Guard Clause: Reject invalid or missing data immediately
            if (payload == null || !(payload is InventoryManager shopInventory))
            {
                Debug.LogWarning("Mage UI attempted to open without a valid InventoryManager payload. Aborting.");
                return;
            }

            // 2. The UI is dumb. It just takes what it was given and displays it.
            if (_shopManagerSO != null)
            {
                _shopManagerSO.CurrentShopInventory = shopInventory;
            }
        }

        private void HandleScreenOpen(ScreenType screenType)
        {
            if (screenType != ScreenType.Mage)
                return;

            EnsureInventoryVisible();
        }

        private void EnsureInventoryVisible()
        {
            if (_uiManager == null || _uiEvents == null || _uiManager.IsWindowOpen(ScreenType.Inventory))
                return;

            _uiEvents.OnRequestOpen?.Invoke(ScreenType.Inventory, null);
        }

        private void Start()
        {
            if (_mageMainTemplate == null || _slotTemplate == null) return;
            if (_gameSession == null) _gameSession = GameSessionSO.LoadDefault();

            TemplateContainer mageInstance = _mageMainTemplate.Instantiate();

            _view = new MageView(mageInstance, _slotTemplate, _shopTemplate, _uiEvents, _uiInventoryEvents, _gameSession, _shopManagerSO, _gameSession.PlayerHUD);
            _view.Initialize();

            _uiManager.RegisterView(_view, ScreenZone.Left);
        }

        private void OnValidate()
        {
            if (_uiEvents == null)
            {
                Debug.LogError($" <color=red>{name}</color> missing UI Events SO", this);
            }
        }
    }
}
