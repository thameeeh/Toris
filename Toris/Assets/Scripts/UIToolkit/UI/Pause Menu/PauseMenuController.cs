using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using OutlandHaven.UIToolkit;

namespace OutlandHaven.UIToolkit
{
    public class PauseMenuController : MonoBehaviour
    {
        [SerializeField] private VisualTreeAsset _pauseTemplate;
        [SerializeField] private UIEventsSO _uiEvents;

        private PauseMenuView _view;
        private UIManager _uiManager;
        private bool _isPaused = false;

        private void Awake()
        {
            _uiManager = FindFirstObjectByType<UIManager>();
        }

        private void OnEnable()
        {
            _uiEvents.OnScreenOpen += HandleScreenOpen;
            _uiEvents.OnScreenClose += HandleScreenClose;
        }

        private void OnDisable()
        {
            _uiEvents.OnScreenOpen -= HandleScreenOpen;
            _uiEvents.OnScreenClose -= HandleScreenClose;
        }

        private void Start()
        {
            if (_pauseTemplate == null || _uiManager == null) return;

            TemplateContainer pauseInstance = _pauseTemplate.Instantiate();
            pauseInstance.style.position = Position.Absolute;
            pauseInstance.style.top = 0;
            pauseInstance.style.bottom = 0;
            pauseInstance.style.left = 0;
            pauseInstance.style.right = 0;

            _view = new PauseMenuView(pauseInstance, _uiEvents);
            _view.Initialize();

            _view.OnResumeClicked += Resume;
            _view.OnSettingsClicked += OpenSettings;
            _view.OnMainMenuClicked += QuitToMainMenu;

            _uiManager.RegisterView(_view, ScreenZone.FullScreen);
        }

        private void HandleScreenOpen(ScreenType screenType)
        {
            if (screenType == ScreenType.PauseMenu) 
            {
                _isPaused = true;
                Time.timeScale = 0f;
            }
        }

        private void HandleScreenClose(ScreenType screenType)
        {
            if (screenType == ScreenType.PauseMenu) 
            {
                _isPaused = false;
                Time.timeScale = 1f;
            }
        }

        private void Resume()
        {
            _uiEvents.OnRequestClose?.Invoke(ScreenType.PauseMenu);
        }

        private void OpenSettings()
        {
            _uiEvents.OnRequestOpen?.Invoke(ScreenType.SettingsModal, null);
        }

        private void QuitToMainMenu()
        {
            // Auto-save before leaving
            Debug.Log("[PauseMenu] Auto-saving progress before quitting to Main Menu...");
            _uiEvents?.OnQuickSaveRequested?.Invoke();

            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f; // Safety reset
            if (_view != null)
            {
                _view.OnResumeClicked -= Resume;
                _view.OnSettingsClicked -= OpenSettings;
                _view.OnMainMenuClicked -= QuitToMainMenu;
                _view.Dispose();
            }
        }
    }
}
