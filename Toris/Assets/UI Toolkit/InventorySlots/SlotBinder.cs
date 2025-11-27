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

    [SerializeField] float CooldownTime = 3f;
    [SerializeField] ResourceData _coinSO;
    [SerializeField] ResourceData _killsSO;

    GameObject _player;
    PlayerInputReader _pInputReader;

    VisualElement _arrowSkill;
    VisualElement _skillOverlay;
    bool _isOnCooldown = false;
    float _timer;
    private void OnEnable()
    {
        if(Inventory.InventoryInstance != null)
            Inventory.InventoryInstance.OnInventoryChanged += UpdateVisuals;

        _pInputReader.OnAbility1Pressed += StartSkillCooldown;
    }
    private void OnDisable()
    {
        if(Inventory.InventoryInstance != null)
            Inventory.InventoryInstance.OnInventoryChanged -= UpdateVisuals;
        
        _pInputReader.OnAbility1Pressed -= StartSkillCooldown;
    }
    void Awake()
    {
        _player = GameObject.FindWithTag("Player");
        _pInputReader = _player.GetComponent<PlayerInputReader>();

        _uiDocument = GetComponent<UIDocument>();
        _root = _uiDocument.rootVisualElement;

        _buttons = _root.Query<Button>().ToList();
        _arrowSkill = _root.Q<VisualElement>("ArrowSkill");
        _skillOverlay = _arrowSkill.Q<VisualElement>("Skill");

        _time = _root.Q<Label>("Time");
        _coins = _root.Q<Label>("Coins");
        _kills = _root.Q<Label>("Kills");

        StartSkillCooldown();
    }

    private void FixedUpdate()
    {
        DateTime currentTime = DateTime.Now;
        _time.text = currentTime.ToString("mm:ss");

        if (_isOnCooldown)
        {
            _timer -= Time.fixedDeltaTime;

            float percentage = _timer / CooldownTime;

            _skillOverlay.style.height = Length.Percent(percentage * 100);

            if (_timer <= 0)
            { 
                _isOnCooldown = false;
                _skillOverlay.style.height = Length.Percent(0);
            }
            _arrowSkill.Q<Label>("ArrowSkillLabel").text = _timer.ToString("F2") + "s";
        }else
            _arrowSkill.Q<Label>("ArrowSkillLabel").text = "";

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

    void StartSkillCooldown() 
    {
        if (_timer <= 0)
        {
            _timer = CooldownTime;
            _isOnCooldown = true;
            _skillOverlay.style.height = Length.Percent(100);
        }
    }
}
