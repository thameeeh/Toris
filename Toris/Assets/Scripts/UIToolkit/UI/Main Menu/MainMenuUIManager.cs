using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace OutlandHaven.UIToolkit
{
    public class MainMenuUIManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private UIEventsSO _uiEvents;

        private List<GameView> _allViews = new List<GameView>();
        private VisualElement _rootZone;

        private void Awake()
        {
            // Just grab the root of the document. No need for complex zones.
            _rootZone = GetComponent<UIDocument>().rootVisualElement;
        }

        private void OnEnable()
        {
            _uiEvents.OnRequestOpen += OpenWindow;
            _uiEvents.OnRequestClose += CloseWindow;
        }

        private void OnDisable()
        {
            _uiEvents.OnRequestOpen -= OpenWindow;
            _uiEvents.OnRequestClose -= CloseWindow;
        }

        public void RegisterView(GameView view)
        {
            _allViews.Add(view);
            _rootZone.Add(view.Root);

            // In the Main Menu, the primary view should be visible immediately
            if (view.ID == ScreenType.MainMenu)
            {
                view.Setup(null);
                view.Show();
            }
            else
            {
                view.Hide();
            }
        }

        private void OpenWindow(ScreenType type, object payload = null)
        {
            GameView view = _allViews.Find(v => v.ID == type);
            if (view == null) return;

            // Close other open windows (mutual exclusivity for modals like Settings)
            foreach (var otherView in _allViews)
            {
                if (otherView != view && !otherView.IsHidden && otherView.ID != ScreenType.MainMenu)
                {
                    otherView.Hide();
                }
            }

            view.Setup(payload);
            view.Show();
        }

        private void CloseWindow(ScreenType type)
        {
            GameView view = _allViews.Find(v => v.ID == type);
            if (view != null && !view.IsHidden)
            {
                view.Hide();
            }
        }
    }
}