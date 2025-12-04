using System;
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
    List<Label> _amountLabels = new();

    private Label _time;
    private Label _coins;
    private Label _kills;

    [SerializeField] ResourceData _coinSO;
    [SerializeField] ResourceData _killsSO;

    GameObject _player;
    PlayerInputReader _pInputReader;
    PlayerAbilityController abilityController;
    VisualElement _arrowSkill;
    VisualElement _skillOverlay;

    VisualElement _arrowSkill2;
    VisualElement _skillOverlay2;
    VisualElement _arrowSkill2Lock;

    bool _isOnCooldown = false;
    float _timer;
    float _currentTime = 0f;

    bool _isOnCooldown2 = false;
    float _timer2;
    float _currentTime2 = 10f;


    private void OnEnable()
    {
        if (Inventory.InventoryInstance != null)
            Inventory.InventoryInstance.OnInventoryChanged += UpdateVisuals;

        if (_pInputReader != null)
            _pInputReader.OnAbility1Pressed += StartSkillCooldown;
    }

    private void OnDisable()
    {
        if (Inventory.InventoryInstance != null)
            Inventory.InventoryInstance.OnInventoryChanged -= UpdateVisuals;

        if (_pInputReader != null)
            _pInputReader.OnAbility1Pressed -= StartSkillCooldown;
    }

    void Awake()
    {
        _player = GameObject.FindWithTag("Player");
        abilityController = _player.GetComponent<PlayerAbilityController>();

        _pInputReader = _player.GetComponent<PlayerInputReader>();

        _uiDocument = GetComponent<UIDocument>();
        _root = _uiDocument.rootVisualElement;

        _buttons = _root.Query<Button>().ToList();

        for (int i = 0; i < _buttons.Count; i++)
        {
            var label = _root.Q<Label>($"lb{i + 1}");
            if (label != null)
            {
                _amountLabels.Add(label);
            }
            else
            {
                Debug.LogWarning($"Could not find Label named 'lb{i + 1}'");
            }
        }

        _arrowSkill = _root.Q<VisualElement>("ArrowSkill");
        _skillOverlay = _arrowSkill.Q<VisualElement>("Skill");

        _arrowSkill2 = _root.Q<VisualElement>("ArrowSkill2");
        _arrowSkill2Lock = _arrowSkill2.Q<VisualElement>("LockIcon2");
        _skillOverlay2 = _arrowSkill2.Q<VisualElement>("Skill2");

        _time = _root.Q<Label>("Time");
        _coins = _root.Q<Label>("Coins");
        _kills = _root.Q<Label>("Kills");


        //StartSkillCooldown();
    }

    private void FixedUpdate()
    {
        _currentTime += Time.deltaTime;
        TimeSpan timeSpan = TimeSpan.FromSeconds(_currentTime);
        _time.text = timeSpan.ToString(@"mm\:ss");

        var ability1 = abilityController != null ? abilityController.Ability1 : null;

        if (_isOnCooldown && ability1 != null && ability1.cooldownSeconds > 0f)
        {
            _timer -= Time.fixedDeltaTime;

            float cooldownTime = ability1.cooldownSeconds;
            float percentage = Mathf.Clamp01(_timer / cooldownTime);

            _skillOverlay.style.height = Length.Percent(percentage * 100f);

            if (_timer <= 0f)
            {
                _isOnCooldown = false;
                _skillOverlay.style.height = Length.Percent(0);
                _timer = 0f;
            }

            _arrowSkill.Q<Label>("ArrowSkillLabel").text = _timer.ToString("F2") + "s";
        }
        else
        {
            _arrowSkill.Q<Label>("ArrowSkillLabel").text = "";
        }

        var ability2 = abilityController != null ? abilityController.Ability2 : null;

        if (ability2 != null && ability2.IsUnlocked(abilityController.AbilityContext))
        {
            _arrowSkill2.Q<Label>().text = "Hold R";
            _skillOverlay2.style.height = Length.Percent(0);

            if (_arrowSkill2Lock != null)
                _arrowSkill2Lock.style.display = DisplayStyle.None;
        }
        else
        {
            _arrowSkill2.Q<Label>().text = "";
            _skillOverlay2.style.height = Length.Percent(100);

            if (_arrowSkill2Lock != null)
                _arrowSkill2Lock.style.display = DisplayStyle.Flex;
        }
    }


    void UpdateVisuals()
    {
        var resourceDict = Inventory.InventoryInstance.GetAllResources();
        List<ResourceData> inventoryItems = resourceDict.Keys.ToList();

        for (int i = 0; i < _buttons.Count; i++)
        {
            Button curremtBtn = _buttons[i]; 
            
            if (i >= _amountLabels.Count) break;
            Label currentLabel = _amountLabels[i];

            if (i < inventoryItems.Count)
            {
                ResourceData data = inventoryItems[i];
                int amount = resourceDict[data];

                curremtBtn.style.backgroundImage = new StyleBackground(data.resourceIcon);
                currentLabel.text = amount.ToString();
            }
            else
            {
                currentLabel.text = "";
                curremtBtn.style.backgroundImage = null;
            }
        }

        _coins.text = Inventory.InventoryInstance.GetResourceAmount(_coinSO).ToString();
        _kills.text = Inventory.InventoryInstance.GetResourceAmount(_killsSO).ToString();
    }

    void StartSkillCooldown()
    {
        if (abilityController == null)
            return;

        var ability1 = abilityController.Ability1;
        if (ability1 == null)
            return;

        float cooldown = ability1.cooldownSeconds;
        if (cooldown <= 0f)
            return;

        if (_timer <= 0f)
        {
            _timer = cooldown;
            _isOnCooldown = true;
            _skillOverlay.style.height = Length.Percent(100);
        }
    }
}
