using UnityEngine.UIElements;

namespace OutlandHaven.UIToolkit
{
    public class ForgeSubView : UIView
    {
        private VisualTreeAsset _slotTemplate;

        public ForgeSubView(VisualElement topElement, VisualTreeAsset slotTemplate)
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
