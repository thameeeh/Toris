using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using System;

public class SlotBinder : MonoBehaviour
{
    UIDocument _uiDocument;
    VisualElement _root;
    List<Button> _buttons = new();

    private Label _time;
    private Label _coins;
    private Label _kills;

    [SerializeField] ResourceData _coinSO;
    [SerializeField] ResourceData _killsSO;
    VisualElement _arrowSkill;

    float _timer = 0;
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
        _arrowSkill = _root.Q<VisualElement>("ArrowSkill");

        _time = _root.Q<Label>("Time");
        _coins = _root.Q<Label>("Coins");
        _kills = _root.Q<Label>("Kills");

        
    }

    private void FixedUpdate()
    {
        DateTime currentTime = DateTime.Now;
        _time.text = currentTime.ToString("mm:ss");

        _timer -= Time.fixedDeltaTime;

        if(_timer <= 0) 
        {
            _timer = 3;
        }
        _arrowSkill.Q<Label>().text = _timer.ToString("F2") + "s";
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
        _coins.text = Inventory.InventoryInstance.GetResourceAmount(_coinSO).ToString();
        _kills.text = Inventory.InventoryInstance.GetResourceAmount(_killsSO).ToString();

    }
}
