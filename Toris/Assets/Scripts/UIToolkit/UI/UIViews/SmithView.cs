using UnityEngine;
using UnityEngine.UIElements;

namespace OutlandHaven.UIToolkit
{
    public class SmithView : GameView
    {
        public override ScreenType ID => ScreenType.Smith;

        private VisualTreeAsset _shopTemplate;
        private VisualElement _shopPanel;

        public SmithView(VisualElement topElement, VisualTreeAsset shopTemplate, UIEventsSO uiEvents)
            : base(topElement, uiEvents)
        {
            _shopTemplate = shopTemplate;
        }

        protected override void SetVisualElements()
        {
            _shopPanel = m_TopElement.Q<VisualElement>("Smith-middle__panel");

            TemplateContainer shopInstance = _shopTemplate.Instantiate();

            _shopPanel.Add(shopInstance);
        }

        public override void Setup(object payload) 
        {
            
        }
    }
}