using UnityEngine.UIElements;

namespace OutlandHaven.Inventory
{
    public class EquipmentSlotView
    {
        private VisualElement _root;
        private Image _icon;
        private Label _qtyLabel;

        public EquipmentSlotView(VisualElement root)
        {
            _root = root;
            _icon = root.Q<Image>("slot-icon");
            _qtyLabel = root.Q<Label>("slot-qty");

            // Equipment slots don't display quantities, so hide the label
            if (_qtyLabel != null)
            {
                _qtyLabel.style.display = DisplayStyle.None;
            }
        }

        public void Update(ItemInstance item)
        {
            if (item == null || item.BaseItem == null)
            {
                _icon.sprite = null;
                _icon.style.display = DisplayStyle.None;
            }
            else
            {
                _icon.sprite = item.BaseItem.Icon;
                _icon.style.display = DisplayStyle.Flex;
            }
        }
    }
}
