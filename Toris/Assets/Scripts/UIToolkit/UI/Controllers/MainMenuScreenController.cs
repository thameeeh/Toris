using OutlandHaven.UIToolkit;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace UIToolkit.UI
{
    public class MainMenuScreenController : MonoBehaviour
    {
        public GameSessionSO gameSession; // will be used for save/load etc.

        [Header("Scene Names")]
        public string VillageSceneName = "MainArea";

        public UIDocument _doc;

        private Button _startGameButton;
        private Button _quitGameButton;

        private void OnEnable()
        {
            _doc = GetComponent<UIDocument>();
            var root = _doc.rootVisualElement;

            // Assuming your UXML has a button named "btn-new-game"
            _startGameButton = root.Q<Button>("btn-start-game");
            _quitGameButton = root.Q<Button>("btn-quit-game");
            if (_startGameButton != null)
            {
                _startGameButton.clicked += OnNewGameClicked;
            }
            if (_quitGameButton != null)
            {
                _quitGameButton.clicked += QuitGame;
            }
        }

        private void OnDisable()
        {
            if (_startGameButton != null)
            {
                _startGameButton.clicked -= OnNewGameClicked;
            }
            if (_quitGameButton != null)
            {
                _quitGameButton.clicked -= QuitGame;
            }
        }

        private void OnNewGameClicked()
        {
            SceneManager.LoadScene(VillageSceneName);
        }

        public void QuitGame()
        {
            // 1. Log a message to verify the button is working in the Editor
            Debug.Log("Quit Game Request Received");

            // 2. If we are running in the Unity Editor, stop playing.
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
        // 3. If we are in a built application, quit the app.
        Application.Quit();
#endif
        }
    }
}