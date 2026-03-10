using UnityEngine.UIElements;

namespace OutlandHaven.UIToolkit
{
    public class SalvageSubView : UIView
    {
        private VisualTreeAsset _slotTemplate;

        public SalvageSubView(VisualElement topElement, VisualTreeAsset slotTemplate)
            : base(topElement)
        {
            _slotTemplate = slotTemplate;
        }

        protected override void SetVisualElements()
        {
            // Cache visual elements here using m_TopElement.Q<VisualElement>("name")
        }

        public override void Setup(object payload = null)
        {
            // Setup dynamic data here
        }

        public override void Show()
        {
            base.Show();
            // Bind events here
        }

        public override void Hide()
        {
            base.Hide();
            // Unbind events here
        }

        public override void Dispose()
        {
            // Cleanup here
            base.Dispose();
        }
    }
}
