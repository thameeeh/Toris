using UnityEngine;
using UnityEngine.UIElements;

namespace OutlandHaven.UIToolkit
{
    public class UIDragManager : MonoBehaviour
    {
        [Tooltip("Assign the same UIDocument used by UIManager here.")]
        [SerializeField] private UIDocument _uiDocument;

        private VisualElement _dragLayer;
        private VisualElement _ghostIcon;

        // Using a Singleton approach for easy global access from InventorySlotView,
        // without needing to pass references through every view layer.
        public static UIDragManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (_uiDocument == null)
            {
                _uiDocument = GetComponent<UIDocument>();
            }

            if (_uiDocument != null && _uiDocument.rootVisualElement != null)
            {
                InitializeDragLayer(_uiDocument.rootVisualElement);
            }
        }

        private void OnEnable()
        {
            // Failsafe initialization if it was late
            if (_dragLayer == null && _uiDocument != null && _uiDocument.rootVisualElement != null)
            {
                InitializeDragLayer(_uiDocument.rootVisualElement);
            }
        }

        private void InitializeDragLayer(VisualElement root)
        {
            // Create a full-screen, absolute positioned layer for dragging
            _dragLayer = new VisualElement();
            _dragLayer.name = "Drag_Layer";
            _dragLayer.style.position = Position.Absolute;
            _dragLayer.style.left = 0;
            _dragLayer.style.right = 0;
            _dragLayer.style.top = 0;
            _dragLayer.style.bottom = 0;
            _dragLayer.pickingMode = PickingMode.Ignore;

            // Make sure it sits on top of everything
            root.Add(_dragLayer);
            _dragLayer.BringToFront();

            // Initialize the ghost icon element
            _ghostIcon = new VisualElement();
            _ghostIcon.name = "Ghost_Icon";
            _ghostIcon.style.position = Position.Absolute;
            _ghostIcon.style.display = DisplayStyle.None;
            _ghostIcon.pickingMode = PickingMode.Ignore; // Crucial: Ignore pointer events!

            _dragLayer.Add(_ghostIcon);
        }

        public void StartDrag(Sprite sprite, Vector2 position, Vector2 size)
        {
            if (_ghostIcon == null || sprite == null) return;

            // Apply styling dynamically
            _ghostIcon.style.backgroundImage = new StyleBackground(sprite);
            _ghostIcon.style.width = size.x;
            _ghostIcon.style.height = size.y;

            // Center the icon on the pointer position
            _ghostIcon.style.left = position.x - (size.x / 2f);
            _ghostIcon.style.top = position.y - (size.y / 2f);

            _ghostIcon.style.display = DisplayStyle.Flex;
        }

        public void UpdateDrag(Vector2 position)
        {
            if (_ghostIcon == null || _ghostIcon.style.display == DisplayStyle.None) return;

            // Update absolute position to follow the pointer
            float width = _ghostIcon.style.width.value.value;
            float height = _ghostIcon.style.height.value.value;

            _ghostIcon.style.left = position.x - (width / 2f);
            _ghostIcon.style.top = position.y - (height / 2f);
        }

        public void StopDrag()
        {
            if (_ghostIcon == null) return;

            _ghostIcon.style.display = DisplayStyle.None;
            _ghostIcon.style.backgroundImage = null;
        }
    }
}