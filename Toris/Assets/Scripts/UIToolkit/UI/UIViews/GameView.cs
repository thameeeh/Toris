using UnityEngine;
using UnityEngine.UIElements;

namespace OutlandHaven.UIToolkit
{
    public abstract class GameView : UIView
    {
        public abstract ScreenType ID { get; }
        protected UIEventsSO UIEvents;
        private bool buttonSfxCallbacksRegistered;
        private Button hoveredSfxButton;
        private float nextButtonHoverSfxTime;

        public GameView(VisualElement topElement, UIEventsSO uiEvents) : base(topElement) 
        {
            UIEvents = uiEvents;
        }

        public override void Initialize()
        {
            if (m_HideOnAwake)
            {
                HideWithoutScreenEvent();
            }

            SetVisualElements();
            RegisterButtonCallbacks();
            RegisterButtonSfxCallbacks();
        }

        public override void Show()
        {
            base.Show();
            UIEvents.OnScreenOpen?.Invoke(ID);
            if (ControllerFeatureGate.IsEnabled)
            {
                FocusFirstInteractiveElement();
            }
        }

        public override void Hide()
        {
            bool wasVisible = !IsHidden;
            base.Hide();

            if (wasVisible)
            {
                UIEvents.OnScreenClose?.Invoke(ID);
            }
        }

        public override void Dispose()
        {
            UnregisterButtonSfxCallbacks();
            base.Dispose();
        }

        private void RegisterButtonSfxCallbacks()
        {
            // SFX-only hook: default UI button feedback is emitted through UIEventsSO.
            // Button/gameplay behavior remains owned by each concrete view/controller.
            if (buttonSfxCallbacksRegistered || m_TopElement == null || UIEvents == null)
            {
                return;
            }

            m_TopElement.RegisterCallback<PointerEnterEvent>(HandleButtonPointerEnter, TrickleDown.TrickleDown);
            m_TopElement.RegisterCallback<PointerLeaveEvent>(HandleButtonPointerLeave, TrickleDown.TrickleDown);
            m_TopElement.RegisterCallback<ClickEvent>(HandleButtonClick, TrickleDown.TrickleDown);
            buttonSfxCallbacksRegistered = true;
        }

        private void UnregisterButtonSfxCallbacks()
        {
            if (!buttonSfxCallbacksRegistered || m_TopElement == null)
            {
                return;
            }

            m_TopElement.UnregisterCallback<PointerEnterEvent>(HandleButtonPointerEnter, TrickleDown.TrickleDown);
            m_TopElement.UnregisterCallback<PointerLeaveEvent>(HandleButtonPointerLeave, TrickleDown.TrickleDown);
            m_TopElement.UnregisterCallback<ClickEvent>(HandleButtonClick, TrickleDown.TrickleDown);
            buttonSfxCallbacksRegistered = false;
            hoveredSfxButton = null;
        }

        private void HideWithoutScreenEvent()
        {
            m_TopElement.style.display = DisplayStyle.None;
        }

        private void HandleButtonPointerEnter(PointerEnterEvent evt)
        {
            Button button = FindButton(evt.target);
            if (button == null || button == hoveredSfxButton)
            {
                return;
            }

            hoveredSfxButton = button;

            if (Time.unscaledTime < nextButtonHoverSfxTime)
            {
                return;
            }

            nextButtonHoverSfxTime = Time.unscaledTime + UIEvents.ButtonHoverCooldownSeconds;
            UIEvents.RequestSfx(UIEvents.ButtonHoverSfxId);
        }

        private void HandleButtonPointerLeave(PointerLeaveEvent evt)
        {
            Button button = FindButton(evt.target);
            if (button == hoveredSfxButton)
            {
                hoveredSfxButton = null;
            }
        }

        private void HandleButtonClick(ClickEvent evt)
        {
            if (FindButton(evt.target) != null)
            {
                UIEvents.RequestSfx(UIEvents.ButtonConfirmSfxId);
            }
        }

        private static Button FindButton(object target)
        {
            if (target is Button button)
            {
                return button;
            }

            return target is VisualElement element ? element.GetFirstAncestorOfType<Button>() : null;
        }

        private void FocusFirstInteractiveElement()
        {
            if (ID == ScreenType.HUD || m_TopElement == null)
            {
                return;
            }

            // Controller navigation needs an initial focused control before Navigate/Submit events can move.
            m_TopElement.schedule.Execute(FocusFirstInteractiveElementNow).ExecuteLater(0);
        }

        private void FocusFirstInteractiveElementNow()
        {
            if (m_TopElement == null || IsHidden)
            {
                return;
            }

            FindFirstInteractiveElement(m_TopElement)?.Focus();
        }

        private static VisualElement FindFirstInteractiveElement(VisualElement root)
        {
            if (root == null || !IsElementAvailable(root))
            {
                return null;
            }

            if (IsInteractiveElement(root))
            {
                return root;
            }

            int childCount = root.hierarchy.childCount;
            for (int i = 0; i < childCount; i++)
            {
                VisualElement match = FindFirstInteractiveElement(root.ElementAt(i));
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private static bool IsInteractiveElement(VisualElement element)
        {
            return element.focusable
                && (element is Button
                    || element is Toggle
                    || element is Slider
                    || element is DropdownField);
        }

        private static bool IsElementAvailable(VisualElement element)
        {
            return element.enabledInHierarchy
                && element.visible
                && element.resolvedStyle.display != DisplayStyle.None;
        }
    }
}
