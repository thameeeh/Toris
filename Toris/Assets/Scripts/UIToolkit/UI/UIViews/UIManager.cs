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
        private Dictionary<GameView, ScreenZone> _viewZones = new Dictionary<GameView, ScreenZone>();

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

        // Call this from your Controllers (e.g. PlayerController) to register themselves
        public void RegisterView(GameView view, ScreenZone zone)
        {
            _allViews.Add(view);
            _viewZones[view] = zone;

            switch (zone)
            {
                case ScreenZone.HUD: _hudZone.Add(view.Root); break;
                case ScreenZone.Left: _leftZone.Add(view.Root); break;
                case ScreenZone.Right: _rightZone.Add(view.Root); break;
            }

            if (view.ID == ScreenType.HUD)
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

            // Close if it's already open
            if (!view.IsHidden)
            {
                CloseWindow(type);
                return;
            }

            // Close any other open view in the same zone (except HUD)
            if (_viewZones.TryGetValue(view, out ScreenZone zone) && zone != ScreenZone.HUD)
            {
                foreach (var otherView in _allViews)
                {
                    if (otherView != view && !otherView.IsHidden && _viewZones.TryGetValue(otherView, out ScreenZone otherZone) && otherZone == zone)
                    {
                        CloseWindow(otherView.ID);
                    }
                }
            }

            view.Setup(payload);

            if(view.ID == ScreenType.Smith || view.ID == ScreenType.Mage) // opens inventory together with smith or mage
            {
                GameView inventory = _allViews.Find(v => v.ID == ScreenType.Inventory);
                if (inventory != null)
                {
                    inventory.Setup(null);
                    inventory.Show();
                }
            }
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