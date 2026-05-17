using UnityEngine;
using UnityEngine.UIElements;
using OutlandHaven.UIToolkit;

namespace OutlandHaven.Inventory
{
    public class InventoryScreenController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private VisualTreeAsset _inventoryMainTemplate; // <--- Drag Inventory.uxml here
        [SerializeField] private VisualTreeAsset _slotTemplate;
        [SerializeField] private GameSessionSO _gameSession;
        [SerializeField] private UIEventsSO _uiEvents;
        [SerializeField] private UIInventoryEventsSO _uiInventoryEvents;

        [Header("Player Data")]
        [SerializeField] private PlayerHUDBridge _playerHUDBridge;

        private PlayerInventoryView _view;
        private UIManager _uiManager;

        void Awake()
        {
            _uiManager = FindFirstObjectByType<UIManager>();
            if (_gameSession == null) _gameSession = GameSessionSO.LoadDefault();
        }

        private void OnEnable()
        {
            if (_inventoryMainTemplate == null)
            {
                Debug.LogError("InventoryScreenController: Main Template is missing!");
                return;
            }
        }

        private void Start()
        {
            if (_gameSession == null) _gameSession = GameSessionSO.LoadDefault();

            TemplateContainer inventoryInstance = _inventoryMainTemplate.Instantiate();

            inventoryInstance.style.flexGrow = 1; // Make it fill the parent container

            _view = new PlayerInventoryView(inventoryInstance, _slotTemplate,
                _gameSession, _uiEvents, _uiInventoryEvents,
                _gameSession.PlayerEquipment, _gameSession.PlayerPotionInventory, _playerHUDBridge);
            _view.Initialize();

            // Register to the RIGHT zone
            _uiManager.RegisterView(_view, ScreenZone.Right);
        }
        private void OnValidate()
        {
            if (_uiEvents == null)
            {
                Debug.LogError($" <color=red>{name}</color> missing UI Events SO", this);
            }
            if (_uiInventoryEvents == null)
            {
                Debug.LogError($" <color=red>{name}</color> missing UI Inventory Events SO", this);
            }
        }
    }
}
