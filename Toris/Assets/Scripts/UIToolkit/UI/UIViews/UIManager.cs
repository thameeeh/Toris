using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine;
using UnityEngine.UIElements;

namespace OutlandHaven.UIToolkit
{
    public class UIManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private UIEventsSO _UIEvents;
        [SerializeField] private bool showHudOnStart = true;

        private List<GameView> _allViews = new List<GameView>();

        private VisualElement _hudZone; 
        private VisualElement _leftZone;
        private VisualElement _rightZone;

        private void Awake()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;

            _hudZone = root.Q<VisualElement>("Layer_HUD");
            _leftZone = root.Q<VisualElement>("Left_Zone");
            _rightZone = root.Q<VisualElement>("Right_Zone");

            if (_hudZone == null || _leftZone == null || _rightZone == null)
            {
                Debug.LogError("UIManager: Could not find Layout Zones in the UIDocument! Check your UXML names.");
            }
        }

        private void OnEnable()
        {
            _UIEvents.OnRequestOpen += OpenWindow;
            _UIEvents.OnRequestClose += CloseWindow;
            _UIEvents.OnRequestCloseAll += CloseAllWindows;
        }

        private void OnDisable()
        {
            _UIEvents.OnRequestOpen -= OpenWindow;
            _UIEvents.OnRequestClose -= CloseWindow;
            _UIEvents.OnRequestCloseAll -= CloseAllWindows;
        }

        private void OnValidate()
        {
            if(_UIEvents == null)
            {
                Debug.LogError($"<color=red>UIEvents</color> {name} is missing, put SO in the inspector!", this);
            }
        }

        private void Update()
        {
            bool isLeft = _leftZone.Q<TemplateContainer>() != null;
            bool isRight = _rightZone.Q<TemplateContainer>() != null;

            Debug.Log($"Left has UI: {isLeft} | Right has UI: {isRight}");
        }

        // Call this from your Controllers (e.g. PlayerController) to register themselves
        public void RegisterView(GameView view, ScreenZone zone)
        {
            _allViews.Add(view);

            switch (zone)
            {
                case ScreenZone.HUD: _hudZone.Add(view.Root); break;
                case ScreenZone.Left: _leftZone.Add(view.Root); break;
                case ScreenZone.Right: _rightZone.Add(view.Root); break;
            }

            if (view.ID == ScreenType.HUD)
            {
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

            // Close if it's already open
            if (!view.IsHidden)
            {
                CloseWindow(type);
                return;
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

        private void CloseAllWindows()
        {
            foreach (var view in _allViews)
            {
                if (view.ID != ScreenType.HUD) view.Hide();
            }
        }
    }
}