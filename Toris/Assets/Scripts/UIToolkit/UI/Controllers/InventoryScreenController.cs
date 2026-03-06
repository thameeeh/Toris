using UnityEngine;
using UnityEngine.UIElements;

namespace OutlandHaven.UIToolkit
{
    public class InventoryScreenController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private VisualTreeAsset _inventoryMainTemplate; // <--- Drag Inventory.uxml here
        [SerializeField] private VisualTreeAsset _slotTemplate;
        [SerializeField] private GameSessionSO _gameSession;
        [SerializeField] private UIEventsSO _uiEvents;
        [SerializeField] private UIInventoryEventsSO _uiInventoryEvents;

        private PlayerInventoryView _view;
        private UIManager _uiManager;

        void Awake()
        {
            _uiManager = FindFirstObjectByType<UIManager>();
        }

        private void OnEnable()
        {
            if (_inventoryMainTemplate == null) return;

            TemplateContainer inventoryInstance = _inventoryMainTemplate.Instantiate();

            _view = new PlayerInventoryView(inventoryInstance, _slotTemplate, _gameSession, _uiEvents, _uiInventoryEvents);

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