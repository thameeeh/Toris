using OutlandHaven.UIToolkit;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIToolkit.UI
{
    public class MageScreenController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private VisualTreeAsset _mageMainTemplate; // <--- Drag Mage.uxml here
        [SerializeField] private UIEventsSO _uiEvents;
        [SerializeField] private UIInventoryEventsSO _uiInventoryEvents;
        [SerializeField] private GameSessionSO _gameSession;
        [SerializeField] private InventoryContainerSO _shopContainer;
        [SerializeField] private ShopManagerSO _shopManagerSO;

        private MageView _view;
        private UIManager _uiManager;

        void Awake()
        {
            _uiManager = FindFirstObjectByType<UIManager>();

            if(_shopManagerSO != null) _shopManagerSO.Initialize();
        }

        private void OnEnable()
        {
            if (_uiEvents != null)
            {
                _uiEvents.OnRequestOpen += HandleRequestOpen;
            }

            if (_mageMainTemplate == null)
            {
                Debug.LogWarning("MageScreenController: Mage Main Template is missing! This is expected during skeleton phase.");
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
            if (screenType == ScreenType.Mage && payload == null)
            {
                if (_shopManagerSO != null && _shopContainer != null)
                {
                    _shopManagerSO.CurrentShopInventory = _shopContainer;
                }
            }
        }

        private void Start()
        {
            VisualElement mageInstance;
            if (_mageMainTemplate == null)
            {
                // Fallback for skeleton testing without a UXML file
                mageInstance = new VisualElement();
            }
            else
            {
                mageInstance = _mageMainTemplate.Instantiate();
            }

            _view = new MageView(mageInstance, _uiEvents, _uiInventoryEvents, _gameSession, _shopContainer, _shopManagerSO);
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