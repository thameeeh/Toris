using OutlandHaven.UIToolkit;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace UIToolkit.UI
{
    public class MainMenuScreenController : MonoBehaviour
    {
        [Header("Data References")]
        public PlayerDataSO playerData;
        public GameSessionSO gameSession; // will be used for save/load etc.

        [Header("Scene Names")]
        public string VillageSceneName = "MainArea";

        private UIDocument _doc;

        private void OnEnable()
        {
            _doc = GetComponent<UIDocument>();
            var root = _doc.rootVisualElement;

            // Assuming your UXML has a button named "btn-new-game"
            var startGameButton = root.Q<Button>("btn-start-game");
            var quitGameButton = root.Q<Button>("btn-quit-game");
            if (startGameButton != null)
            {
                startGameButton.clicked += OnNewGameClicked;
            }
            if (quitGameButton != null)
            {
                quitGameButton.clicked += QuitGame;
            }
        }

        private void OnNewGameClicked()
        {
            // 1. Wipe the memory (Fresh start)
            playerData.ResetToDefaults();

            // 3. Load the World
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