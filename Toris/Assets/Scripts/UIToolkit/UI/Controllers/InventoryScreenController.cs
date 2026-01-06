using UnityEngine;
using UnityEngine.UIElements;

namespace OutlandHaven.UIToolkit
{
    public class InventoryScreenController : MonoBehaviour
    {

        private InventoryView _view;
        private UIManager _uiManager;

        void Awake()
        {
            _uiManager = FindFirstObjectByType<UIManager>();
        }

        private void OnEnable()
        {
            var uiDoc = GetComponent<UIDocument>();
            _view = new InventoryView(uiDoc.rootVisualElement);

            _uiManager.RegisterView(_view);
        }
    }
}