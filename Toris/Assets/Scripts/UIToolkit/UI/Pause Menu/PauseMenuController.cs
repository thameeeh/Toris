using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using OutlandHaven.UIToolkit;

namespace OutlandHaven.UIToolkit
{
    public class PauseMenuController : MonoBehaviour
    {
        private const string MainAreaSceneName = "MainArea";
        private const string MainMenuSceneName = "MainMenu";

        [SerializeField] private VisualTreeAsset _pauseTemplate;
        [SerializeField] private UIEventsSO _uiEvents;

        private PauseMenuView _view;
        private UIManager _uiManager;
        private bool _isPaused = false;
        private bool _isStatsPanelOpen = false;
        private bool _isItemsPanelOpen = false;

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
            _view.OnAdventureLogClicked += ToggleAdventureLog;
            _view.OnSettingsClicked += OpenSettings;
            _view.OnMainMenuClicked += QuitToMainMenu;
            _view.OnToggleItemsClicked += ToggleItemsPanel;

            _uiManager.RegisterView(_view, ScreenZone.FullScreen);
        }

        private void HandleScreenOpen(ScreenType screenType)
        {
            if (screenType == ScreenType.PauseMenu) 
            {
                _isPaused = true;
                Time.timeScale = 0f;
                _isStatsPanelOpen = false;
                _isItemsPanelOpen = false;
                if (_view != null)
                {
                    _view.SetStatsPanelActive(false);
                    _view.SetItemsPanelActive(false);
                }
            }
        }

        private void HandleScreenClose(ScreenType screenType)
        {
            if (screenType == ScreenType.PauseMenu) 
            {
                _isPaused = false;
                Time.timeScale = 1f;
                _isStatsPanelOpen = false;
                _isItemsPanelOpen = false;
                if (_view != null)
                {
                    _view.SetStatsPanelActive(false);
                    _view.SetItemsPanelActive(false);
                }
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

        private void ToggleAdventureLog()
        {
            _isStatsPanelOpen = !_isStatsPanelOpen;
            _view.SetStatsPanelActive(_isStatsPanelOpen);

            if (!_isStatsPanelOpen)
            {
                _isItemsPanelOpen = false;
                _view.SetItemsPanelActive(false);
            }

            if (_isStatsPanelOpen)
            {
                UpdateStatsDisplay();
            }
        }

        private void ToggleItemsPanel()
        {
            if (!_isStatsPanelOpen) return;
            _isItemsPanelOpen = !_isItemsPanelOpen;
            _view.SetItemsPanelActive(_isItemsPanelOpen);
        }

        private void UpdateStatsDisplay()
        {
            var session = GameSessionSO.LoadDefault();
            int totalKills = 0;
            int wolfKills = 0;
            float playtimeSeconds = 0f;
            int totalPickUps = 0;
            System.Collections.Generic.Dictionary<string, int> resolvedItemNamesAndCounts = new System.Collections.Generic.Dictionary<string, int>();

            if (session != null && session.GameplayStatistics != null)
            {
                totalKills = session.GameplayStatistics.TotalKills;
                wolfKills = session.GameplayStatistics.WolfKills;
                playtimeSeconds = session.GameplayStatistics.PlayTime;
                totalPickUps = session.GameplayStatistics.TotalPickUps;

                // Resolve item names from MasterItemDatabase inside SaveManager in scene
                OutlandHaven.SaveSystem.SaveManager saveManager = FindFirstObjectByType<OutlandHaven.SaveSystem.SaveManager>();
                OutlandHaven.Inventory.ItemDatabaseSO database = saveManager != null ? saveManager.MasterItemDatabase : null;
                if (database != null && session.GameplayStatistics.ItemPickUps != null)
                {
                    database.Initialize();
                    foreach (var kvp in session.GameplayStatistics.ItemPickUps)
                    {
                        OutlandHaven.Inventory.InventoryItemSO blueprint = database.GetItemByID(kvp.Key);
                        string displayName = blueprint != null ? blueprint.ItemName : kvp.Key;
                        if (!string.IsNullOrEmpty(displayName))
                        {
                            resolvedItemNamesAndCounts[displayName] = kvp.Value;
                        }
                    }
                }
            }

            int totalSeconds = Mathf.RoundToInt(playtimeSeconds);
            int hours = totalSeconds / 3600;
            int minutes = (totalSeconds % 3600) / 60;
            int seconds = totalSeconds % 60;
            string playtimeString = string.Format("{0:00}:{1:00}:{2:00}", hours, minutes, seconds);

            _view.PopulateStats(totalKills, wolfKills, playtimeString, totalPickUps, resolvedItemNamesAndCounts);
        }

        private void QuitToMainMenu()
        {
            if (SceneManager.GetActiveScene().name == MainAreaSceneName)
            {
                // Save/procedural transfer related: only hub quits are save points.
#if UNITY_EDITOR
                Debug.Log("[PauseMenu] Auto-saving MainArea progress before quitting to Main Menu...");
#endif
                _uiEvents?.OnQuickSaveRequested?.Invoke();
            }
#if UNITY_EDITOR
            else
            {
                Debug.Log("[PauseMenu] Skipped Main Menu auto-save outside MainArea.");
            }
#endif

            Time.timeScale = 1f;
            if (SceneTransitionService.Instance != null)
            {
                SceneTransitionService.Instance.LoadScene(MainMenuSceneName);
                return;
            }

            SceneManager.LoadScene(MainMenuSceneName);
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f; // Safety reset
            if (_view != null)
            {
                _view.OnResumeClicked -= Resume;
                _view.OnAdventureLogClicked -= ToggleAdventureLog;
                _view.OnSettingsClicked -= OpenSettings;
                _view.OnMainMenuClicked -= QuitToMainMenu;
                _view.OnToggleItemsClicked -= ToggleItemsPanel;
                _view.Dispose();
            }
        }
    }
}
