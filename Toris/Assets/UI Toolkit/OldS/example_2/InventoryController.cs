using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class InventoryController : MonoBehaviour
{
    UIDocument UIDocument => GetComponent<UIDocument>();
    private void Start()
    {
        VisualElement container = UIDocument.rootVisualElement;

        // Create some list of data, here simply numbers in interval [1, 1000]
        const int itemCount = 1000;
        var items = new List<string>(itemCount);
        for (var i = 0; i < itemCount; i++)
            items.Add(i.ToString());

        // The "makeItem" function will be called as needed
        // when the ListView needs more items to render
        Func<VisualElement> makeItem = () => new Label();

        // As the user scrolls through the list, the ListView object
        // will recycle elements created by the "makeItem"
        // and invoke the "bindItem" callback to associate
        // the element with the matching data item(specified as an index in the list)
        Action<VisualElement, int> bindItem = (e, i) => ((Label)e).text = items[i];

        var listView = container.Q<ListView>();
        listView.makeItem = makeItem;
        listView.bindItem = bindItem;
        listView.itemsSource = items;
        listView.selectionType = SelectionType.Multiple;

        // Callback invoked when the user double clicks an item
        listView.itemsChosen += (selectedItems) =>
        {
            Debug.Log("Items chosen: " + string.Join(", ", selectedItems));
        };

        // Callback invoked when the user changes the selection inside the ListView
        listView.selectedIndicesChanged += (selectedIndices) =>
        {
            Debug.Log("Index selected: " + string.Join(", ", selectedIndices));

            // Note: selectedIndices can also be used to get the selected items from the itemsSource directly or
            // by using listView.viewController.GetItemForIndex(index).
        };
    }
}
