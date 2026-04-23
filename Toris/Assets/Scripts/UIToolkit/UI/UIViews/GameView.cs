using UnityEngine;
using UnityEngine.UIElements;

namespace OutlandHaven.UIToolkit
{
    public abstract class GameView : UIView
    {
        public abstract ScreenType ID { get; }
        protected UIEventsSO UIEvents;

        public GameView(VisualElement topElement, UIEventsSO uiEvents) : base(topElement) 
        {
            UIEvents = uiEvents;
        }

        public override void Show()
        {
            base.Show();
            UIEvents.OnScreenOpen?.Invoke(ID);
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
    }
}
