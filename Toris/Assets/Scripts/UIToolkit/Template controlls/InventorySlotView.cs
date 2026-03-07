using UnityEngine;
using UnityEngine.UIElements;

namespace OutlandHaven.UIToolkit
{
    public class InventorySlotView
    {
        private VisualElement _root;
        private Image _icon;
        private Label _qtyLabel;

        public InventorySlotView(VisualElement root)
        {
            _root = root;
            _icon = root.Q<Image>("slot-icon");
            _qtyLabel = root.Q<Label>("slot-qty");
        }

        public void Update(InventorySlot slotData)
        {
            if (slotData == null || slotData.IsEmpty)
            {
                _icon.sprite = null;
                _icon.style.display = DisplayStyle.None;
                _qtyLabel.text = "";
            }
            else
            {
                _icon.sprite = slotData.Item.Icon;
                _icon.style.display = DisplayStyle.Flex;
                _qtyLabel.text = slotData.Count > 1 ? slotData.Count.ToString() : "";
            }
        }
    }
}