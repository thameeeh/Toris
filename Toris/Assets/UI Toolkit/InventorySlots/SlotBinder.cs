using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class SlotBinder : MonoBehaviour
{
    UIDocument _uiDocument;
    VisualElement _root;
    List<Button> _buttons = new();



    private void OnEnable()
    {
        if(Inventory.InventoryInstance != null)
            Inventory.InventoryInstance.OnInventoryChanged += UpdateVisuals;
    }
    private void OnDisable()
    {
        if(Inventory.InventoryInstance != null)
            Inventory.InventoryInstance.OnInventoryChanged -= UpdateVisuals;
    }
    void Awake()
    {
        _uiDocument = GetComponent<UIDocument>();
        _root = _uiDocument.rootVisualElement;

        _buttons = _root.Query<Button>().ToList();
    }
    
    void UpdateVisuals()
    {
        var resourceDict = Inventory.InventoryInstance.GetAllResources();

        List<ResourceData> inventoryItems = resourceDict.Keys.ToList();

        for (int i = 0; i < _buttons.Count; i++)
        {
            Button curremtBtn = _buttons[i];

            if (i < inventoryItems.Count)
            {
                ResourceData data = inventoryItems[i];
                int amount = resourceDict[data];

                curremtBtn.style.backgroundImage = new StyleBackground(data.resourceIcon);

                curremtBtn.text = amount.ToString();
            }
            else
            {
                curremtBtn.text = "";
                curremtBtn.style.backgroundImage = null;
            }
        }
    }
}
