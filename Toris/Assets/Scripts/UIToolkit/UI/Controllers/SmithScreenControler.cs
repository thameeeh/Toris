using OutlandHaven.UIToolkit;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIToolkit.UI
{
    public class SmithScreenController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private VisualTreeAsset _slotTemplate; // <--- DRAG Slot.uxml HERE
        [SerializeField] private UIEventsSO _uiEvents;

        private SmithView _view;
        private UIManager _uiManager;

        void Awake()
        {
            _uiManager = FindFirstObjectByType<UIManager>();
        }

        private void OnEnable()
        {
            if (_slotTemplate == null)
            {
                Debug.LogError("SmithScreenController: Slot Template is missing!");
                return;
            }

            var uiDoc = GetComponent<UIDocument>();
            
            // Pass the Template and the GameSession (for player data) to the View
            _view = new SmithView(uiDoc.rootVisualElement, _slotTemplate, _uiEvents);
            
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