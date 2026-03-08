using OutlandHaven.UIToolkit;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIToolkit.UI
{
    public class SmithScreenController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private VisualTreeAsset _smithMainTemplate; // Smith.uxml
        [SerializeField] private VisualTreeAsset _shopTemplate;
        [SerializeField] private UIEventsSO _uiEvents;

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
            if (_shopTemplate == null)
            {
                Debug.LogError("SmithScreenController: Slot Template is missing!");
                return;
            }

        }

        private void Start()
        {
            if (_smithMainTemplate == null || _shopTemplate == null) return;

            TemplateContainer smithInstance = _smithMainTemplate.Instantiate();
            _view = new SmithView(smithInstance, _shopTemplate, _uiEvents);

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