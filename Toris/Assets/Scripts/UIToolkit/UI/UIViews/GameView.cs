using UnityEngine;
using UnityEngine.UIElements;

namespace OutlandHaven.UIToolkit
{
    public abstract class GameView : UIView
    {
        public abstract ScreenType ID { get; }

        public GameView(VisualElement topElement) : base(topElement) { }

        public override void Show()
        {
            base.Show();
            UIEvents.OnScreenOpen?.Invoke(ID);
        }
    }
}