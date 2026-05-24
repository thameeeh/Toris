using OutlandHaven.Inventory;
using UnityEngine;
using UnityEngine.UIElements;

namespace OutlandHaven.UIToolkit
{
    public sealed class ItemTooltipView
    {
        private const float CursorOffset = 18f;
        private const float ScreenPadding = 12f;
        private const float FallbackWidth = 460f;
        private const float FallbackBaseHeight = 168f;
        private const float FallbackRowHeight = 38f;

        private readonly VisualElement _host;
        private readonly VisualElement _root;
        private readonly Label _titleLabel;
        private readonly Label _descriptionLabel;
        private readonly VisualElement _rowsContainer;

        private Vector2 _lastPointerPosition;
        private int _lastRowCount;

        public ItemTooltipView(VisualElement host)
        {
            _host = host;
            _root = new VisualElement { name = "ItemTooltip" };
            _root.AddToClassList("item-tooltip");
            _root.pickingMode = PickingMode.Ignore;

            _titleLabel = new Label { name = "ItemTooltipTitle" };
            _titleLabel.AddToClassList("item-tooltip__title");

            _descriptionLabel = new Label { name = "ItemTooltipDescription" };
            _descriptionLabel.AddToClassList("item-tooltip__description");

            _rowsContainer = new VisualElement { name = "ItemTooltipRows" };
            _rowsContainer.AddToClassList("item-tooltip__rows");

            _root.Add(_titleLabel);
            _root.Add(_descriptionLabel);
            _root.Add(_rowsContainer);
            _host.Add(_root);

            Hide();
        }

        public void Show(ItemTooltipData data, Vector2 pointerPosition)
        {
            if (data == null)
            {
                Hide();
                return;
            }

            _titleLabel.text = data.Title;
            _descriptionLabel.text = data.Description;
            RebuildRows(data);

            _root.style.display = DisplayStyle.Flex;
            Move(pointerPosition);
            _root.schedule.Execute(() => Move(_lastPointerPosition)).ExecuteLater(0);
        }

        public void Move(Vector2 pointerPosition)
        {
            if (_root.style.display == DisplayStyle.None)
                return;

            _lastPointerPosition = pointerPosition;

            float tooltipWidth = ResolveTooltipWidth();
            float tooltipHeight = ResolveTooltipHeight();
            float hostWidth = ResolveDimension(_host.resolvedStyle.width, Screen.width);
            float hostHeight = ResolveDimension(_host.resolvedStyle.height, Screen.height);

            float left = pointerPosition.x + CursorOffset;
            float top = pointerPosition.y + CursorOffset;

            if (left + tooltipWidth + ScreenPadding > hostWidth)
                left = pointerPosition.x - tooltipWidth - CursorOffset;

            if (top + tooltipHeight + ScreenPadding > hostHeight)
                top = pointerPosition.y - tooltipHeight - CursorOffset;

            left = Mathf.Clamp(left, ScreenPadding, Mathf.Max(ScreenPadding, hostWidth - tooltipWidth - ScreenPadding));
            top = Mathf.Clamp(top, ScreenPadding, Mathf.Max(ScreenPadding, hostHeight - tooltipHeight - ScreenPadding));

            _root.style.left = left;
            _root.style.top = top;
        }

        public void Hide()
        {
            _root.style.display = DisplayStyle.None;
        }

        private void RebuildRows(ItemTooltipData data)
        {
            _rowsContainer.Clear();
            _lastRowCount = data.Rows.Count;

            for (int i = 0; i < data.Rows.Count; i++)
            {
                ItemTooltipRow row = data.Rows[i];
                VisualElement rowElement = new VisualElement();
                rowElement.AddToClassList("item-tooltip__row");

                Label label = new Label(row.Label);
                label.AddToClassList("item-tooltip__row-label");

                Label value = new Label(row.Value);
                value.AddToClassList("item-tooltip__row-value");

                rowElement.Add(label);
                rowElement.Add(value);
                _rowsContainer.Add(rowElement);
            }
        }

        private float ResolveTooltipWidth()
        {
            return ResolveDimension(_root.resolvedStyle.width, FallbackWidth);
        }

        private float ResolveTooltipHeight()
        {
            float fallback = FallbackBaseHeight + (_lastRowCount * FallbackRowHeight);
            return ResolveDimension(_root.resolvedStyle.height, fallback);
        }

        private static float ResolveDimension(float value, float fallback)
        {
            return float.IsNaN(value) || value <= 0f ? fallback : value;
        }
    }
}
