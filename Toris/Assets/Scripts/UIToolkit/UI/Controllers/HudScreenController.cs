using UnityEngine;
using UnityEngine.UIElements;

namespace OutlandHaven.UIToolkit
{
    [RequireComponent(typeof(UIDocument))]
    public class HudScreenController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private GameSessionSO _gameSession; // Access to data

        private HUDView _view;
        private UIManager _uiManager;

        void Awake()
        {
            _uiManager = FindFirstObjectByType<UIManager>();
        }

        private void OnEnable()
        {
            if (_gameSession == null || _gameSession.PlayerData == null)
            {
                Debug.LogError("HUD Controller is missing GameSession or PlayerData!");
                return;
            }

            var uiDoc = GetComponent<UIDocument>();

            // Pass the data into the View constructor
            _view = new HUDView(uiDoc.rootVisualElement, _gameSession.PlayerData);

            _uiManager.RegisterView(_view);
        }
    }
}