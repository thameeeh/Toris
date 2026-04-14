using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using OutlandHaven.Inventory;

namespace OutlandHaven.UIToolkit
{
    public class ShopSubView : UIView
    {
        private VisualTreeAsset _slotTemplate;
        private InventoryManager _shopContainer;
        private UIInventoryEventsSO _uiInventoryEvents;
        private GameSessionSO _gameSession;
        private PlayerHUDBridge _playerHudBridge;

        private VisualElement _shopGrid;
        private Label _goldAmountLabel;

        private List<InventorySlotView> _slotViews = new List<InventorySlotView>();

        private bool _isSetup = false;
        private bool _eventsBound = false;

        private const int BULK_BUY_AMOUNT = 10;

        public ShopSubView(VisualElement topElement, VisualTreeAsset slotTemplate, UIInventoryEventsSO uiInventoryEvents, GameSessionSO gameSession, PlayerHUDBridge playerHudBridge)
            : base(topElement)
        {
            _slotTemplate = slotTemplate;
            _uiInventoryEvents = uiInventoryEvents;
            _gameSession = gameSession;
            _playerHudBridge = playerHudBridge;
        }

        protected override void SetVisualElements()
        {
            // Set VisualElements
            _shopGrid = m_TopElement.Q<VisualElement>("shop-grid");
            _goldAmountLabel = m_TopElement.Q<Label>("gold-amount-label");

            if (_shopGrid == null)
            {
#if UNITY_EDITOR
                Debug.LogError("ShopSubView: shop-grid not found in UXML.");
#endif
                return;
            }
        }

        public override void Setup(object payload = null)
        {
            if (payload is InventoryManager dynamicShopContainer)
            {
                _shopContainer = dynamicShopContainer;
            }

            // Always recreate slots when setup is called to ensure we have the latest payload data
            CreateSlots();

            // Bind initial gold
            if (_gameSession != null && _playerHudBridge != null)
            {
                UpdateGoldAmount(_playerHudBridge.CurrentGold);
            }

            _isSetup = true;
        }

        public override void Show()
        {
            base.Show();
            // Listen to Events
            if (!_eventsBound && _uiInventoryEvents != null)
            {
                if (_playerHudBridge != null) _playerHudBridge.OnGoldChanged += HandleGoldChanged;
                _uiInventoryEvents.OnShopInventoryUpdated += HandleShopInventoryUpdated;
                _uiInventoryEvents.OnItemRightClicked += HandleItemRightClicked;
                _eventsBound = true;
            }
        }

        public override void Hide()
        {
            base.Hide();
            if (_eventsBound && _uiInventoryEvents != null)
            {
                if (_playerHudBridge != null) _playerHudBridge.OnGoldChanged -= HandleGoldChanged;
                _uiInventoryEvents.OnShopInventoryUpdated -= HandleShopInventoryUpdated;
                _uiInventoryEvents.OnItemRightClicked -= HandleItemRightClicked;
                _eventsBound = false;
            }
        }

        private void CreateSlots()
        {
            // Clean up old subscriptions to prevent memory leaks and ghost updates
            foreach (var view in _slotViews)
            {
                view.Dispose();
            }

            _shopGrid.Clear();
            _slotViews.Clear();

            if (_shopContainer == null) return;

            for (int i = 0; i < _shopContainer.LiveSlots.Count; i++)
            {
                TemplateContainer slotInstance = _slotTemplate.Instantiate();
                _shopGrid.Add(slotInstance);

                var slotView = new InventorySlotView(slotInstance, _shopContainer, _uiInventoryEvents);
                var slotData = _shopContainer.LiveSlots[i];

                slotView.Update(slotData);
                _slotViews.Add(slotView);
            }
        }

        private void HandleItemRightClicked(InventorySlot slotData)
        {
            if (slotData == null || slotData.IsEmpty) return;

            bool isShiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            int amount = isShiftHeld ? BULK_BUY_AMOUNT : 1;

            // Check if clicking an item in the shop
            if (_shopContainer != null && _shopContainer.LiveSlots.Contains(slotData))
            {
                _uiInventoryEvents?.OnRequestBuy?.Invoke(slotData.HeldItem, amount);
                return;
            }

            // Check if clicking an item in the player inventory
            if (_gameSession != null && _gameSession.PlayerInventory != null && _gameSession.PlayerInventory.LiveSlots.Contains(slotData))
            {
                _uiInventoryEvents?.OnRequestSell?.Invoke(slotData.HeldItem, amount);
                return;
            }
        }

        private void HandleShopInventoryUpdated()
        {
            if (_shopContainer == null) return;

            // Recreate slots to handle any size changes in the inventory container
            CreateSlots();
        }

                private void HandleGoldChanged(int currentGold, int delta)
        {
            UpdateGoldAmount(currentGold);
        }

        private void UpdateGoldAmount(int amount)
        {
            if (_goldAmountLabel != null)
            {
                _goldAmountLabel.text = amount.ToString();
            }
        }

        public override void Dispose()
        {
            if (_eventsBound && _uiInventoryEvents != null)
            {
                if (_playerHudBridge != null) _playerHudBridge.OnGoldChanged -= HandleGoldChanged;
                _uiInventoryEvents.OnShopInventoryUpdated -= HandleShopInventoryUpdated;
                _uiInventoryEvents.OnItemRightClicked -= HandleItemRightClicked;
                _eventsBound = false;
            }
            base.Dispose();
        }
    }
}
