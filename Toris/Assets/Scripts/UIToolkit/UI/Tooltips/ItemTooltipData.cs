using System.Collections.Generic;

namespace OutlandHaven.Inventory
{
    public sealed class ItemTooltipData
    {
        private readonly List<ItemTooltipRow> _rows = new();

        public string Title { get; }
        public string Description { get; }
        public IReadOnlyList<ItemTooltipRow> Rows => _rows;

        public ItemTooltipData(string title, string description)
        {
            Title = title;
            Description = description;
        }

        public void AddRow(string label, string value)
        {
            if (string.IsNullOrWhiteSpace(label) || string.IsNullOrWhiteSpace(value))
                return;

            _rows.Add(new ItemTooltipRow(label, value));
        }
    }

    public readonly struct ItemTooltipRow
    {
        public string Label { get; }
        public string Value { get; }

        public ItemTooltipRow(string label, string value)
        {
            Label = label;
            Value = value;
        }
    }
}
