using UnityEngine;
using UnityEngine.UIElements;

namespace OutlandHaven.UIToolkit
{
    public class InventoryScreenController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private VisualTreeAsset _slotTemplate; // <--- DRAG Slot.uxml HERE
        [SerializeField] private GameSessionSO _gameSession;
        [SerializeField] private UIEventsSO _uiEvents;

        private InventoryView _view;
        private UIManager _uiManager;

        void Awake()
        {
            _uiManager = FindFirstObjectByType<UIManager>();
        }

        private void OnEnable()
        {
            if (_slotTemplate == null)
            {
                Debug.LogError("InventoryScreenController: Slot Template is missing!");
                return;
            }

            var uiDoc = GetComponent<UIDocument>();

            // Pass the Template and the GameSession (for player data) to the View
            _view = new InventoryView(uiDoc.rootVisualElement, _slotTemplate, _gameSession, _uiEvents);

            _uiManager.RegisterView(_view);
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