using OutlandHaven.UIToolkit;
using UnityEngine;
using UnityEngine.UIElements;
using OutlandHaven.Inventory;

namespace OutlandHaven.UIToolkit
{
    public class SmithScreenController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private VisualTreeAsset _smithMainTemplate; // <--- Drag Smith.uxml here
        [SerializeField] private VisualTreeAsset _slotTemplate; // <--- DRAG Slot.uxml HERE
        [SerializeField] private VisualTreeAsset _shopTemplate; // <--- DRAG ShopSubView.uxml HERE
        [SerializeField] private VisualTreeAsset _forgeTemplate; // <--- DRAG ForgeSubView_Smith.uxml HERE
        [SerializeField] private VisualTreeAsset _salvageTemplate; // <--- DRAG SalvageSubView_Smith.uxml HERE
        [SerializeField] private UIEventsSO _uiEvents;
        [SerializeField] private UIInventoryEventsSO _uiInventoryEvents;
        [SerializeField] private GameSessionSO _gameSession;
        [SerializeField] private InventoryContainerSO _shopContainer;
        [SerializeField] private ShopManagerSO _shopManagerSO;
        [SerializeField] private CraftingManagerSO _craftingManagerSO;
        [SerializeField] private SalvageManagerSO _salvageManagerSO;

        private SmithView _view;
        private UIManager _uiManager;

        void Awake()
        {
            _uiManager = FindFirstObjectByType<UIManager>();

            if(_shopManagerSO != null) _shopManagerSO.Initialize();
            if(_craftingManagerSO != null) _craftingManagerSO.Initialize();
            if(_salvageManagerSO != null) _salvageManagerSO.Initialize();
        }

        private void OnEnable()
        {
            if (_uiEvents != null)
            {
                _uiEvents.OnRequestOpen += HandleRequestOpen;
            }

            if (_smithMainTemplate == null)
            {
                Debug.LogError("SmithScreenController: Smith Main Template is missing!");
                return;
            }
            if (_slotTemplate == null)
            {
                Debug.LogError("SmithScreenController: Slot Template is missing!");
                return;
            }
        }

        private void OnDisable()
        {
            if (_uiEvents != null)
            {
                _uiEvents.OnRequestOpen -= HandleRequestOpen;
            }
        }

        private void HandleRequestOpen(ScreenType screenType, object payload)
        {
            // If the UI is opened via shortcut keys (payload is null)
            // we manually provide the default container so the ShopManager knows which inventory to use.
            if (screenType == ScreenType.Smith && payload == null)
            {
                if (_shopManagerSO != null && _shopContainer != null)
                {
                    _shopManagerSO.CurrentShopInventory = _shopContainer;
                }
            }
        }

        private void Start()
        {
            if (_smithMainTemplate == null || _slotTemplate == null) return;

            TemplateContainer smithInstance = _smithMainTemplate.Instantiate();
            _view = new SmithView(smithInstance, _slotTemplate, _shopTemplate, _forgeTemplate, _salvageTemplate, _uiEvents, _uiInventoryEvents, _gameSession, _shopContainer, _craftingManagerSO, _salvageManagerSO);
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