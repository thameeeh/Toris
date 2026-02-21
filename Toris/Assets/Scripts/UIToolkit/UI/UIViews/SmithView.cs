using UnityEngine;
using UnityEngine.UIElements;

namespace OutlandHaven.UIToolkit
{
    public class SmithView : GameView
    {
        public override ScreenType ID => ScreenType.Smith;

        private VisualTreeAsset _slotTemplate;

        private VisualElement _externalGrid;
        private VisualElement _externalPanel; // The whole right side
        private Label _externalHeader;

        public SmithView(VisualElement topElement, VisualTreeAsset slotTemplate, UIEventsSO uiEvents)
            : base(topElement, uiEvents)
        {
            _slotTemplate = slotTemplate;
        }

        protected override void SetVisualElements()
        {
            _externalGrid = m_TopElement.Q<VisualElement>("grid-external");

            // Find the containers for toggling visibility
            _externalPanel = m_TopElement.Q<VisualElement>("container__external");
            _externalHeader = m_TopElement.Q<Label>("label__external-header");
        }

        public override void Setup(object payload) 
        {
            
        }
    }
}