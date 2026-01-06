using UnityEngine;
using UnityEngine.UIElements;

namespace OutlandHaven.UIToolkit 
{ 
    public class PlayerInfoView : UIView
    {
        private Button _healButton;

        public VisualElement StatContainer { get; private set; }
        public PlayerInfoView(VisualElement topElement) : base(topElement) 
        {
        }

        public override void Dispose()
        {
        }

        protected override void SetVisualElements()
        {
            _healButton = m_TopElement.Q<Button>("Heal-Button");

            StatContainer = m_TopElement.Q<VisualElement>("StatListContainer");
        }

        protected override void RegisterButtonCallbacks()
        {
            _healButton.RegisterCallback<ClickEvent>(SelectHealButton);
        }

        void SelectHealButton(ClickEvent evt)
        {
            PlayerEvents.OnHealRequested?.Invoke(10);
        }
    }
}