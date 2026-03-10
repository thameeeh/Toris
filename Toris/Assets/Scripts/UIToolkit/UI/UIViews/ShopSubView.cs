using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace OutlandHaven.UIToolkit
{
    public class ShopSubView : UIView
    {
        private VisualTreeAsset _slotTemplate;
        private InventoryContainerSO _shopContainer;
        private UIInventoryEventsSO _uiInventoryEvents;
        private GameSessionSO _gameSession;

        private VisualElement _shopGrid;
        private Label _goldAmountLabel;

        private List<InventorySlotView> _slotViews = new List<InventorySlotView>();

        private bool _isSetup = false;
        private bool _eventsBound = false;

        public ShopSubView(VisualElement topElement, VisualTreeAsset slotTemplate, InventoryContainerSO shopContainer, UIInventoryEventsSO uiInventoryEvents, GameSessionSO gameSession)
            : base(topElement)
        {
            _slotTemplate = slotTemplate;
            _shopContainer = shopContainer;
            _uiInventoryEvents = uiInventoryEvents;
            _gameSession = gameSession;
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
            if (payload is InventoryContainerSO dynamicShopContainer)
            {
                _shopContainer = dynamicShopContainer;
            }

            // Always recreate slots when setup is called to ensure we have the latest payload data
            CreateSlots();

            // Bind initial gold
            if (_gameSession != null && _gameSession.PlayerData != null)
            {
                UpdateGoldAmount(_gameSession.PlayerData.Gold);
            }

            _isSetup = true;
        }

        public override void Show()
        {
            base.Show();
            // Listen to Events
            if (!_eventsBound && _uiInventoryEvents != null)
            {
                _uiInventoryEvents.OnCurrencyChanged += UpdateGoldAmount;
                _uiInventoryEvents.OnShopInventoryUpdated += HandleShopInventoryUpdated;
                _eventsBound = true;
            }
        }

        public override void Hide()
        {
            base.Hide();
            if (_eventsBound && _uiInventoryEvents != null)
            {
                _uiInventoryEvents.OnCurrencyChanged -= UpdateGoldAmount;
                _uiInventoryEvents.OnShopInventoryUpdated -= HandleShopInventoryUpdated;
                _eventsBound = false;
            }
        }

        private void CreateSlots()
        {
            _shopGrid.Clear();
            _slotViews.Clear();

            if (_shopContainer == null) return;

            for (int i = 0; i < _shopContainer.Slots.Count; i++)
            {
                TemplateContainer slotInstance = _slotTemplate.Instantiate();
                _shopGrid.Add(slotInstance);

                var slotView = new InventorySlotView(slotInstance);
                var slotData = _shopContainer.Slots[i];

                slotView.Update(slotData);
                _slotViews.Add(slotView);

                // Register buy interaction on Right Click (ContextClickEvent)
                var currentSlotData = slotData; // Capture variable for lambda

                // We register on the slot instance root so the player can click anywhere in the slot
                slotInstance.RegisterCallback<MouseUpEvent>(evt =>
                {
                    if (evt.button == 1)
                    {
                        if (currentSlotData != null && !currentSlotData.IsEmpty)
                        {
                            int amount = evt.shiftKey ? 10 : 1;
                            // Only request buy, let the manager handle logic and update UI
                            _uiInventoryEvents?.OnRequestBuy?.Invoke(currentSlotData.Item, amount);
                        }
                    }
                });
            }
        }

        private void HandleShopInventoryUpdated()
        {
            if (_shopContainer == null) return;

            // Recreate slots to handle any size changes in the inventory container
            CreateSlots();
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
                _uiInventoryEvents.OnCurrencyChanged -= UpdateGoldAmount;
                _uiInventoryEvents.OnShopInventoryUpdated -= HandleShopInventoryUpdated;
                _eventsBound = false;
            }
            base.Dispose();
        }
    }
}
