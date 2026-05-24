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
        [SerializeField] private VisualTreeAsset _slotTemplate; // <--- DRAG simple Slot.uxml HERE
        [SerializeField] private VisualTreeAsset _shopSlotTemplate; // <--- DRAG NEW ShopSlot.uxml HERE
        [SerializeField] private VisualTreeAsset _shopTemplate; // <--- DRAG ShopSubView.uxml HERE
        [SerializeField] private VisualTreeAsset _forgeTemplate; // <--- DRAG ForgeSubView_Smith.uxml HERE
        [SerializeField] private VisualTreeAsset _salvageTemplate; // <--- DRAG SalvageSubView_Smith.uxml HERE
        [SerializeField] private UIEventsSO _uiEvents;
        [SerializeField] private UIInventoryEventsSO _uiInventoryEvents;
        [SerializeField] private GameSessionSO _gameSession;
        [SerializeField] private ShopManagerSO _shopManagerSO;
        [SerializeField] private CraftingManagerSO _craftingManagerSO;
        [SerializeField] private SalvageManagerSO _salvageManagerSO;

        private SmithView _view;
        private UIManager _uiManager;

        void Awake()
        {
            _uiManager = FindFirstObjectByType<UIManager>();
            if (_gameSession == null) _gameSession = GameSessionSO.LoadDefault();

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
            if (screenType != ScreenType.Smith) return;

            // 1. Resolve payload dynamically if it's null or invalid
            InventoryManager shopInventory = payload as InventoryManager;
            shopInventory = PlayerInventorySceneResolver.ResolveShopInventory(ScreenType.Smith, shopInventory);

            if (shopInventory == null)
            {
                Debug.LogWarning("Smith UI attempted to open without a valid InventoryManager payload or fallback. Aborting.");
                return;
            }

            // 2. The UI is dumb. It just takes what it was given and displays it.
            if (_shopManagerSO != null)
            {
                _shopManagerSO.CurrentShopInventory = shopInventory;
            }

            if (_view != null)
            {
                _view.Setup(shopInventory);
            }
        }

        private void Start()
        {
            if (_smithMainTemplate == null || _slotTemplate == null) return;
            if (_gameSession == null) _gameSession = GameSessionSO.LoadDefault();

            TemplateContainer smithInstance = _smithMainTemplate.Instantiate();

            smithInstance.style.flexGrow = 1; // Make it fill the available space

            _view = new SmithView(smithInstance, _slotTemplate, _shopSlotTemplate, _shopTemplate, _forgeTemplate, _salvageTemplate, _uiEvents, _uiInventoryEvents, _gameSession, _gameSession.PlayerHUD, _craftingManagerSO, _salvageManagerSO);
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