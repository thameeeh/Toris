using OutlandHaven.UIToolkit;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIToolkit.UI
{
    public class SmithScreenController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private VisualTreeAsset _smithMainTemplate; // <--- Drag Smith.uxml here
        [SerializeField] private VisualTreeAsset _slotTemplate; // <--- DRAG Slot.uxml HERE
        [SerializeField] private VisualTreeAsset _shopTemplate; // <--- DRAG ShopSubView.uxml HERE
        [SerializeField] private UIEventsSO _uiEvents;
        [SerializeField] private UIInventoryEventsSO _uiInventoryEvents;
        [SerializeField] private GameSessionSO _gameSession;
        [SerializeField] private InventoryContainerSO _shopContainer;

        private SmithView _view;
        private UIManager _uiManager;

        void Awake()
        {
            _uiManager = FindFirstObjectByType<UIManager>();
        }

        private void OnEnable()
        {
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

        private void Start()
        {
            if (_smithMainTemplate == null || _slotTemplate == null) return;

            TemplateContainer smithInstance = _smithMainTemplate.Instantiate();
            _view = new SmithView(smithInstance, _slotTemplate, _shopTemplate, _uiEvents, _uiInventoryEvents, _gameSession, _shopContainer);
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