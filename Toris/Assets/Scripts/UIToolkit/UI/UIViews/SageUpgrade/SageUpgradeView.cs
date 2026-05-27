using UnityEngine;
using UnityEngine.UIElements;
using OutlandHaven.Inventory;
using System;

namespace OutlandHaven.UIToolkit
{
    public class SageUpgradeView : GameView
    {
        public override ScreenType ID => ScreenType.SageUpgrade;

        private UIInventoryEventsSO _uiInventoryEvents;
        private GameSessionSO _gameSession;
        private UpgradeSalvageManagerSO _upgradeManager;
        private PlayerHUDBridge _playerHudBridge;
        private InventorySlot _selectedSlot;

        // Subview properties
        private VisualTreeAsset _upgradeSubViewTemplate;
        private VisualTreeAsset _slotTemplate;
        private VisualElement _middlePanel;

        // Input Slot View Elements
        private VisualElement _inputSlotContainer;
        private InventorySlotView _inputSlotView;

        // Visual Elements cached from UXML
        private Label _weaponNameLabel;
        private Label _levelBeforeLabel;
        private Label _levelAfterLabel;
        private Label _statBeforeLabel;
        private Label _statAfterLabel;
        private Label _upgradeCostLabel;
        private Label _playerGoldLabel;
        private Button _btnUpgrade;

        private bool _eventsBound = false;

        public SageUpgradeView(
            VisualElement topElement,
            VisualTreeAsset upgradeSubViewTemplate,
            VisualTreeAsset slotTemplate,
            UIEventsSO uiEvents,
            UIInventoryEventsSO uiInventoryEvents,
            GameSessionSO gameSession,
            UpgradeSalvageManagerSO upgradeManager)
            : base(topElement, uiEvents)
        {
            _upgradeSubViewTemplate = upgradeSubViewTemplate;
            _slotTemplate = slotTemplate;
            _uiInventoryEvents = uiInventoryEvents;
            _gameSession = gameSession;
            _upgradeManager = upgradeManager;
            if (gameSession != null)
            {
                _playerHudBridge = gameSession.PlayerHUD;
            }

            // Instantiates the subview UI elements into the tab middle panel
            _middlePanel = m_TopElement.Q<VisualElement>("Sage-middle__panel");
            if (_middlePanel != null && _upgradeSubViewTemplate != null)
            {
                TemplateContainer subviewInstance = _upgradeSubViewTemplate.Instantiate();
                subviewInstance.style.flexGrow = 1;
                _middlePanel.Add(subviewInstance);
            }
            else
            {
                Debug.LogError("SageUpgradeView: Failed to find Sage-middle__panel or subview template is missing!");
            }
        }

        protected override void SetVisualElements()
        {
            if (m_TopElement == null) return;

            _inputSlotContainer = m_TopElement.Q<VisualElement>("sage-upgrade-slot-container");
            _weaponNameLabel = m_TopElement.Q<Label>("weapon-name-label");
            _levelBeforeLabel = m_TopElement.Q<Label>("level-before-label");
            _levelAfterLabel = m_TopElement.Q<Label>("level-after-label");
            _statBeforeLabel = m_TopElement.Q<Label>("stat-before-label");
            _statAfterLabel = m_TopElement.Q<Label>("stat-after-label");
            _upgradeCostLabel = m_TopElement.Q<Label>("upgrade-cost-label");
            _playerGoldLabel = m_TopElement.Q<Label>("player-gold-label");
            _btnUpgrade = m_TopElement.Q<Button>("btn-upgrade");

            // Setup input slot view (using the global Slot.uxml template)
            if (_inputSlotContainer != null && _slotTemplate != null)
            {
                TemplateContainer instance = _slotTemplate.Instantiate();
                instance.pickingMode = PickingMode.Ignore;
                instance.userData = "sage-upgrade-input"; // Dynamic proxy ID for dragging/dropping
                _inputSlotContainer.Add(instance);
                _inputSlotView = new InventorySlotView(instance, null, _uiInventoryEvents);

                instance.RegisterCallback<MouseUpEvent>(evt =>
                {
                    if (evt.button == 0) // Left click to remove item
                    {
                        ClearInputSlot();
                    }
                });
            }
        }

        protected override void RegisterButtonCallbacks()
        {
            _btnUpgrade?.RegisterCallback<ClickEvent>(OnUpgradeClicked);
        }

        private void OnUpgradeClicked(ClickEvent evt)
        {
            if (_selectedSlot != null && !_selectedSlot.IsEmpty)
            {
                _uiInventoryEvents?.OnRequestSageUpgrade?.Invoke(_selectedSlot);
            }
        }

        public override void Setup(object payload)
        {
            // Re-validate the selected slot after an upgrade is successfully triggered
            if (_selectedSlot != null)
            {
                bool exists = false;
                if (_gameSession != null && _gameSession.PlayerInventory != null)
                {
                    foreach (var slot in _gameSession.PlayerInventory.LiveSlots)
                    {
                        if (slot == _selectedSlot && !slot.IsEmpty)
                        {
                            exists = true;
                            break;
                        }
                    }
                }

                if (!exists)
                {
                    SelectSlot(null);
                }
                else
                {
                    SelectSlot(_selectedSlot); // Refresh preview with upgraded item
                }
            }
            else
            {
                SelectSlot(null);
            }
        }

        public override void Show()
        {
            base.Show();
            
            // Set inventory context to SageUpgrade to allow clicking/dragging from inventory
            _uiInventoryEvents?.OnInteractionContextChanged?.Invoke(InventoryInteractionContext.SageUpgrade);
            
            if (!_eventsBound && _uiInventoryEvents != null)
            {
                _uiInventoryEvents.OnItemClicked += HandleItemClicked;
                _uiInventoryEvents.OnRequestSelectForProcessing += HandleProxyDrop;
                _eventsBound = true;
            }

            UpdatePlayerGoldText();
            
            if (_selectedSlot == null || _selectedSlot.IsEmpty)
            {
                SelectSlot(null);
            }
            else
            {
                SelectSlot(_selectedSlot);
            }

            if (_playerHudBridge != null)
            {
                _playerHudBridge.OnGoldChanged += HandleGoldChanged;
            }
        }

        public override void Hide()
        {
            base.Hide();
            
            // Restore inventory context to Normal
            _uiInventoryEvents?.OnInteractionContextChanged?.Invoke(InventoryInteractionContext.Normal);
            
            if (_eventsBound && _uiInventoryEvents != null)
            {
                _uiInventoryEvents.OnItemClicked -= HandleItemClicked;
                _uiInventoryEvents.OnRequestSelectForProcessing -= HandleProxyDrop;
                _eventsBound = false;
            }

            if (_playerHudBridge != null)
            {
                _playerHudBridge.OnGoldChanged -= HandleGoldChanged;
            }

            // Proactively clear input slot on close
            ClearInputSlot();
        }

        private void HandleGoldChanged(int currentGold, int delta)
        {
            UpdatePlayerGoldText();
            RefreshUpgradePreview();
        }

        private void HandleProxyDrop(InventorySlot sourceSlot, string slotID)
        {
            if (slotID == "sage-upgrade-input")
            {
                if (sourceSlot == null || sourceSlot.IsEmpty) return;

                // Validate if it is upgradeable!
                var upgradeComp = sourceSlot.HeldItem.BaseItem.GetComponent<UpgradeableComponent>();
                if (upgradeComp == null)
                {
                    Debug.LogWarning("SageUpgradeView: Selected item is not upgradeable!");
                    return;
                }

                SelectSlot(sourceSlot);
            }
        }

        private void HandleItemClicked(InventorySlot slot)
        {
            if (slot == null || slot.IsEmpty) return;

            // Validate if it is upgradeable!
            var upgradeComp = slot.HeldItem.BaseItem.GetComponent<UpgradeableComponent>();
            if (upgradeComp == null)
            {
                Debug.LogWarning("SageUpgradeView: Selected item is not upgradeable!");
                return;
            }

            SelectSlot(slot);
        }

        private void SelectSlot(InventorySlot slot)
        {
            _selectedSlot = slot;

            if (slot != null && !slot.IsEmpty)
            {
                // Set item visually in the input slot (displaying 1 item)
                InventorySlot proxySlot = new InventorySlot();
                proxySlot.SetItem(new ItemInstance(slot.HeldItem.BaseItem), 1);
                _inputSlotView?.Update(proxySlot);
            }
            else
            {
                _inputSlotView?.Update(null);
            }

            RefreshUpgradePreview();
        }

        private void ClearInputSlot()
        {
            SelectSlot(null);
        }

        private void RefreshUpgradePreview()
        {
            if (_selectedSlot == null || _selectedSlot.IsEmpty)
            {
                if (_weaponNameLabel != null) _weaponNameLabel.text = "Select weapon";
                if (_levelBeforeLabel != null) _levelBeforeLabel.text = "-";
                if (_levelAfterLabel != null) _levelAfterLabel.text = "-";
                if (_statBeforeLabel != null) _statBeforeLabel.text = "-";
                if (_statAfterLabel != null) _statAfterLabel.text = "-";
                if (_upgradeCostLabel != null) _upgradeCostLabel.text = "0 Gold";
                if (_btnUpgrade != null)
                {
                    _btnUpgrade.text = "Upgrade Weapon";
                    _btnUpgrade.SetEnabled(false);
                }
                return;
            }

            var item = _selectedSlot.HeldItem;
            var upgradeComp = item.BaseItem.GetComponent<UpgradeableComponent>();
            var upgradeState = item.GetState<UpgradeableState>();

            if (upgradeComp == null || upgradeState == null)
            {
                return;
            }

            int currentLevel = upgradeState.CurrentLevel;
            int maxLevel = upgradeComp.MaxLevel;
            bool isMax = currentLevel >= maxLevel;

            if (_weaponNameLabel != null)
            {
                _weaponNameLabel.text = $"{item.BaseItem.ItemName} (+{currentLevel})";
            }

            if (_levelBeforeLabel != null) _levelBeforeLabel.text = "+" + currentLevel;
            if (_levelAfterLabel != null) _levelAfterLabel.text = isMax ? "MAX" : "+" + (currentLevel + 1);

            // Compute Stats
            WeaponComputedStats stats = EquippedItemStatCalculator.CalculateWeapon(item);
            float currentDmg = stats.FinalWeaponDamage;
            
            // Damage increases by 2 per upgrade level
            float nextDmg = currentDmg + 2f;

            if (_statBeforeLabel != null) _statBeforeLabel.text = currentDmg.ToString("F0");
            if (_statAfterLabel != null) _statAfterLabel.text = isMax ? "-" : nextDmg.ToString("F0");

            int cost = _upgradeManager != null ? _upgradeManager.CalculateUpgradeCost(item) : 0;
            if (_upgradeCostLabel != null) _upgradeCostLabel.text = cost.ToString() + " Gold";

            UpdatePlayerGoldText();

            if (_btnUpgrade != null)
            {
                if (isMax)
                {
                    _btnUpgrade.text = "Max Level Reached";
                    _btnUpgrade.SetEnabled(false);
                }
                else
                {
                    int playerGold = GetPlayerGold();
                    if (playerGold < cost)
                    {
                        _btnUpgrade.text = "Insufficient Gold";
                        _btnUpgrade.SetEnabled(false);
                    }
                    else
                    {
                        _btnUpgrade.text = "Upgrade Weapon";
                        _btnUpgrade.SetEnabled(true);
                    }
                }
            }
        }

        private void UpdatePlayerGoldText()
        {
            if (_playerGoldLabel == null) return;
            _playerGoldLabel.text = GetPlayerGold().ToString() + " Gold";
        }

        private int GetPlayerGold()
        {
            if (_upgradeManager != null && _upgradeManager.PlayerAnchor != null && _upgradeManager.PlayerAnchor.IsReady)
            {
                return _upgradeManager.PlayerAnchor.Instance.CurrentGold;
            }
            if (_playerHudBridge != null)
            {
                return _playerHudBridge.CurrentGold;
            }
            return 0;
        }

        public override void Dispose()
        {
            if (_eventsBound && _uiInventoryEvents != null)
            {
                _uiInventoryEvents.OnItemClicked -= HandleItemClicked;
                _uiInventoryEvents.OnRequestSelectForProcessing -= HandleProxyDrop;
                _eventsBound = false;
            }
            if (_playerHudBridge != null)
            {
                _playerHudBridge.OnGoldChanged -= HandleGoldChanged;
            }
            base.Dispose();
        }
    }
}
