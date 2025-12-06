using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class InventoryController : MonoBehaviour
{
    [Header("UI Setup")]
    [SerializeField] private UIDocument _document;

    // DRAG 'inventory_item.uxml' HERE IN INSPECTOR
    [SerializeField] private VisualTreeAsset _itemTemplate;

    // Simple Data Class
    public class ItemData
    {
        public string Name;
        public Color IconColor; // simulating an icon sprite
    }

    private List<ItemData> _inventoryData;
    private ListView _listView;

    void Start()
    {
        // 1. Create Fake Data
        _inventoryData = new List<ItemData>
        {
            new ItemData { Name = "Iron Sword", IconColor = Color.red },
            new ItemData { Name = "Steel Shield", IconColor = Color.blue },
            new ItemData { Name = "Health Potion", IconColor = Color.green },
            new ItemData { Name = "Mana Potion", IconColor = Color.cyan },
            new ItemData { Name = "Old Map", IconColor = Color.yellow }
        };

        // 2. Find the ListView
        var root = _document.rootVisualElement;
        _listView = root.Q<ListView>("MyInventoryList");

        // 3. Configure the ListView

        // A. Set the Data Source
        _listView.itemsSource = _inventoryData;

        // B. Set the "makeItem" (Architecture)
        // If you dragged the UXML to the Inspector, you don't strictly need this line,
        // but it's good practice to have a code fallback or assign it here explicitly.
        if (_itemTemplate != null)
        {
            _listView.makeItem = () => _itemTemplate.Instantiate();
        }

        // C. Set the "bindItem" (The Data Bridge)
        _listView.bindItem = (VisualElement element, int index) =>
        {
            // 'element' is the instantiated 'inventory_item.uxml'
            var data = _inventoryData[index];

            // Update Text
            var label = element.Q<Label>("ItemName");
            label.text = data.Name;

            // Update Icon (Color)
            var icon = element.Q<VisualElement>("Icon");
            icon.style.backgroundColor = data.IconColor;
        };

        // D. Optional: Handle Selection
        _listView.selectionChanged += OnItemSelected;
    }

    private void OnItemSelected(IEnumerable<object> selectedItems)
    {
        // Since we can select multiple, this returns a list. 
        // We just grab the first one for this example.
        foreach (var item in selectedItems)
        {
            var data = item as ItemData;
            Debug.Log($"Selected: {data.Name}");
        }
    }
}
