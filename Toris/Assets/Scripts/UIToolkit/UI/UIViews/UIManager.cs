using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine;

namespace OutlandHaven.UIToolkit
{
    public class UIManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private bool showHudOnStart = true;

        private List<GameView> _allViews = new List<GameView>();

        private GameView _currentActiveWindow;
        private GameView _hudView;

        private void OnEnable()
        {
            UIEvents.OnRequestOpen += OpenWindow;
            UIEvents.OnRequestClose += CloseWindow;
            UIEvents.OnRequestCloseAll += CloseAllWindows;
        }

        private void OnDisable()
        {
            UIEvents.OnRequestOpen -= OpenWindow;
            UIEvents.OnRequestClose -= CloseWindow;
            UIEvents.OnRequestCloseAll -= CloseAllWindows;
        }

        // Call this from your Controllers (e.g. PlayerController) to register themselves
        public void RegisterView(GameView view)
        {
            if (view.ID == ScreenType.HUD)
            {
                _hudView = view;
                if (showHudOnStart) _hudView.Show();
            }
            else
            {
                _allViews.Add(view);
                view.Hide(); // Default to hidden
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (_currentActiveWindow != null)
                {
                    CloseWindow(_currentActiveWindow.ID);
                }
                else
                {
                    OpenWindow(ScreenType.PauseMenu);
                }
            }

            // Inventory Shortcut (I)
            if (Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame)
            {
                UIEvents.OnRequestOpen?.Invoke(ScreenType.Inventory);
            }

            // Character Sheet Shortcut (C)
            if (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame)
            {
                UIEvents.OnRequestOpen?.Invoke(ScreenType.CharacterSheet);
            }
        }

        private void OpenWindow(ScreenType type)
        {
            // 1. Find the view requested
            GameView view = _allViews.Find(v => v.ID == type);
            if (view == null) return;

            // 2. Logic: If it's already open, toggle it closed (Diablo style behavior)
            if (_currentActiveWindow == view)
            {
                CloseWindow(type);
                return;
            }

            // 3. Logic: If another window is open (e.g. Inventory), close it first?
            // (Or keep both open if you support multi-window)
            if (_currentActiveWindow != null)
            {
                _currentActiveWindow.Hide();
            }

            // 4. Show the new window
            view.Show();
            _currentActiveWindow = view;

            // Optional: Pause Game?
            // Time.timeScale = 0; 
        }

        private void CloseWindow(ScreenType type)
        {
            GameView view = _allViews.Find(v => v.ID == type);
            if (view != null)
            {
                view.Hide();
                if (_currentActiveWindow == view) _currentActiveWindow = null;
            }
        }

        private void CloseAllWindows()
        {
            foreach (var view in _allViews)
            {
                view.Hide();
            }
            _currentActiveWindow = null;
        }
    }
}