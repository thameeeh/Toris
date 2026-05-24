using System.Collections.Generic;
using OutlandHaven.Inventory;
using OutlandHaven.Skills;
using UnityEngine;
using UnityEngine.UIElements;

namespace OutlandHaven.UIToolkit
{
    public class UIManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private UIEventsSO _UIEvents;
        [SerializeField] private UIInventoryEventsSO _UIInventoryEvents;
        [SerializeField] private UISkillEventsSO _UISkillEvents;
        private const string DefaultInventoryEventsResourcePath = "GameData/SOForEvents/UI Inventory Events SO";
        private const string DefaultSkillEventsResourcePath = "GameData/SOForEvents/UI Skill Events SO";

        private List<GameView> _allViews = new List<GameView>();
        private Dictionary<GameView, ScreenZone> _viewZones = new Dictionary<GameView, ScreenZone>();
        private ItemTooltipView _itemTooltipView;
        private bool _tooltipEventsBound;
        private bool _inventoryDragActive;

        private VisualElement _hudZone; 
        private VisualElement _leftZone;
        private VisualElement _rightZone;
        private VisualElement _fullScreen_Zone;

        private void Awake()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            ResolveInventoryEvents();
            ResolveSkillEvents();

            _hudZone = root.Q<VisualElement>("Layer_HUD");
            _leftZone = root.Q<VisualElement>("Left_Zone");
            _rightZone = root.Q<VisualElement>("Right_Zone");
            _fullScreen_Zone = root.Q<VisualElement>("FullScreen_Zone");
            _itemTooltipView = new ItemTooltipView(root);

            if (_hudZone == null || _leftZone == null || _rightZone == null || _fullScreen_Zone == null)
            {
                Debug.LogError("UIManager: Could not find Layout Zones in the UIDocument! Check your UXML names.");
            }
        }

        private void OnEnable()
        {
            _UIEvents.OnRequestOpen += OpenWindow;
            _UIEvents.OnRequestClose += CloseWindow;
            _UIEvents.OnRequestCloseAll += CloseAllWindows;
            ResolveInventoryEvents();
            ResolveSkillEvents();
            BindTooltipEvents();
        }

        private void OnDisable()
        {
            _UIEvents.OnRequestOpen -= OpenWindow;
            _UIEvents.OnRequestClose -= CloseWindow;
            _UIEvents.OnRequestCloseAll -= CloseAllWindows;
            UnbindTooltipEvents();
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
                case ScreenZone.FullScreen: _fullScreen_Zone.Add(view.Root); break;
                case ScreenZone.Modal: _fullScreen_Zone.Add(view.Root); break;
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
            HandleItemTooltipHide();

            GameView view = _allViews.Find(v => v.ID == type);
            if (view == null) return;

            // Close if it's already open
            if (!view.IsHidden)
            {
                if (IsNonDismissibleScreen(type))
                    return;

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
            view.Show();
            view.Root.BringToFront();
        }

        private void CloseWindow(ScreenType type)
        {
            if (IsNonDismissibleScreen(type))
                return;

            HandleItemTooltipHide();

            GameView view = _allViews.Find(v => v.ID == type);
            if (view != null && !view.IsHidden)
            {
                view.Hide();
            }
        }

        public void CloseAllWindows()
        {
            HandleItemTooltipHide();

            foreach (var view in _allViews)
            {
                if (view.ID != ScreenType.HUD && !IsNonDismissibleScreen(view.ID)) view.Hide();
            }
        }

        public bool IsAnyWindowOpen()
        {
            foreach (var view in _allViews)
            {
                if (view.ID != ScreenType.HUD && !view.IsHidden) return true;
            }
            return false;
        }

        public bool IsWindowOpen(ScreenType type)
        {
            GameView view = _allViews.Find(v => v.ID == type);
            return view != null && !view.IsHidden;
        }

        private void ResolveInventoryEvents()
        {
            if (_UIInventoryEvents != null)
                return;

            _UIInventoryEvents = Resources.Load<UIInventoryEventsSO>(DefaultInventoryEventsResourcePath);
        }

        private void ResolveSkillEvents()
        {
            if (_UISkillEvents != null)
                return;

            _UISkillEvents = Resources.Load<UISkillEventsSO>(DefaultSkillEventsResourcePath);
        }

        private void BindTooltipEvents()
        {
            if (_tooltipEventsBound || _UIInventoryEvents == null)
                return;

            _UIInventoryEvents.OnItemTooltipShow += HandleItemTooltipShow;
            _UIInventoryEvents.OnItemTooltipMove += HandleItemTooltipMove;
            _UIInventoryEvents.OnItemTooltipHide += HandleItemTooltipHide;
            _UIInventoryEvents.OnGlobalDragStarted += HandleGlobalDragStarted;
            _UIInventoryEvents.OnGlobalDragStopped += HandleGlobalDragStopped;

            if (_UISkillEvents != null)
            {
                _UISkillEvents.OnAbilityTooltipShow += HandleAbilityTooltipShow;
                _UISkillEvents.OnAbilityTooltipMove += HandleAbilityTooltipMove;
                _UISkillEvents.OnAbilityTooltipHide += HandleItemTooltipHide;
            }

            _tooltipEventsBound = true;
        }

        private void UnbindTooltipEvents()
        {
            if (!_tooltipEventsBound || _UIInventoryEvents == null)
                return;

            _UIInventoryEvents.OnItemTooltipShow -= HandleItemTooltipShow;
            _UIInventoryEvents.OnItemTooltipMove -= HandleItemTooltipMove;
            _UIInventoryEvents.OnItemTooltipHide -= HandleItemTooltipHide;
            _UIInventoryEvents.OnGlobalDragStarted -= HandleGlobalDragStarted;
            _UIInventoryEvents.OnGlobalDragStopped -= HandleGlobalDragStopped;

            if (_UISkillEvents != null)
            {
                _UISkillEvents.OnAbilityTooltipShow -= HandleAbilityTooltipShow;
                _UISkillEvents.OnAbilityTooltipMove -= HandleAbilityTooltipMove;
                _UISkillEvents.OnAbilityTooltipHide -= HandleItemTooltipHide;
            }

            _tooltipEventsBound = false;
        }

        private void HandleItemTooltipShow(InventorySlot slot, Vector2 pointerPosition)
        {
            if (_inventoryDragActive)
                return;

            ItemTooltipData data = ItemTooltipFormatter.Build(slot);
            _itemTooltipView?.Show(data, pointerPosition);
        }

        private void HandleItemTooltipMove(Vector2 pointerPosition)
        {
            if (_inventoryDragActive)
                return;

            _itemTooltipView?.Move(pointerPosition);
        }

        private void HandleAbilityTooltipShow(PlayerAbilitySlotSnapshot snapshot, Vector2 pointerPosition)
        {
            if (_inventoryDragActive)
                return;

            ItemTooltipData data = AbilityTooltipFormatter.Build(snapshot);
            _itemTooltipView?.Show(data, pointerPosition);
        }

        private void HandleAbilityTooltipMove(Vector2 pointerPosition)
        {
            if (_inventoryDragActive)
                return;

            _itemTooltipView?.Move(pointerPosition);
        }

        private void HandleItemTooltipHide()
        {
            _itemTooltipView?.Hide();
        }

        private void HandleGlobalDragStarted(Sprite sprite, Vector2 pointerPosition, Vector2 iconSize)
        {
            _inventoryDragActive = true;
            HandleItemTooltipHide();
        }

        private void HandleGlobalDragStopped()
        {
            _inventoryDragActive = false;
        }

        private static bool IsNonDismissibleScreen(ScreenType screenType)
        {
            // Death screen must survive Escape / CloseAll so the player cannot
            // soft-dismiss it while dead.
            return screenType == ScreenType.DeathScreen;
        }
    }
}
