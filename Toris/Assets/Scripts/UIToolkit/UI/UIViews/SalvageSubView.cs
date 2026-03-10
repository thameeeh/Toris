using UnityEngine;
using UnityEngine.UIElements;

namespace OutlandHaven.UIToolkit
{
    public class SalvageSubView : UIView
    {
        private VisualTreeAsset _slotTemplate;
        private UIInventoryEventsSO _uiInventoryEvents;
        private CraftingRegistrySO _registry;

        private VisualElement _inputSlotContainer;
        private InventorySlotView _inputSlotView;
        private InventorySlot _currentSlotData;

        private TextField _goldYieldField;
        private VisualElement _itemYieldContainer;
        private InventorySlotView _itemYieldView;

        private Button _btnGetGold;
        private Button _btnGetItem;

        private bool _eventsBound = false;

        public SalvageSubView(VisualElement topElement, VisualTreeAsset slotTemplate, UIInventoryEventsSO uiInventoryEvents, CraftingRegistrySO registry)
            : base(topElement)
        {
            _slotTemplate = slotTemplate;
            _uiInventoryEvents = uiInventoryEvents;
            _registry = registry;
        }

        protected override void SetVisualElements()
        {
            _inputSlotContainer = m_TopElement.Q<VisualElement>("salvage-slot-input");

            _goldYieldField = m_TopElement.Q<TextField>("salvage-gold-field");
            _itemYieldContainer = m_TopElement.Q<VisualElement>("salvage-item-yield");

            _btnGetGold = m_TopElement.Q<Button>("btn-get-gold");
            _btnGetItem = m_TopElement.Q<Button>("btn-get-item");

            // Setup the views
            if (_inputSlotContainer != null)
            {
                TemplateContainer instance = _slotTemplate.Instantiate();
                _inputSlotContainer.Add(instance);
                _inputSlotView = new InventorySlotView(instance);

                instance.RegisterCallback<MouseUpEvent>(evt =>
                {
                    if (evt.button == 0) // Left click to remove item
                    {
                        ClearInputSlot();
                    }
                });
            }

            if (_itemYieldContainer != null)
            {
                TemplateContainer instance = _slotTemplate.Instantiate();
                _itemYieldContainer.Add(instance);
                _itemYieldView = new InventorySlotView(instance);
            }
        }

        public override void Setup(object payload = null)
        {
            ClearInputSlot();
            UpdateYieldVisuals();
        }

        public override void Show()
        {
            base.Show();
            if (!_eventsBound && _uiInventoryEvents != null)
            {
                _uiInventoryEvents.OnItemClicked += HandleItemClicked;
                _eventsBound = true;
            }

            _btnGetGold?.RegisterCallback<ClickEvent>(OnBtnGetGoldClicked);
            _btnGetItem?.RegisterCallback<ClickEvent>(OnBtnGetItemClicked);
        }

        public override void Hide()
        {
            base.Hide();
            if (_eventsBound && _uiInventoryEvents != null)
            {
                _uiInventoryEvents.OnItemClicked -= HandleItemClicked;
                _eventsBound = false;
            }

            _btnGetGold?.UnregisterCallback<ClickEvent>(OnBtnGetGoldClicked);
            _btnGetItem?.UnregisterCallback<ClickEvent>(OnBtnGetItemClicked);
        }

        private void HandleItemClicked(InventorySlot slot)
        {
            // Set the clicked item into the slot visually
            _currentSlotData = slot;

            // To display correctly in the visual slot view which might expect an InventorySlot
            _inputSlotView?.Update(slot);

            UpdateYieldVisuals();
        }

        private void ClearInputSlot()
        {
            _currentSlotData = null;
            _inputSlotView?.Update(null);
            UpdateYieldVisuals();
        }

        private void UpdateYieldVisuals()
        {
            if (_currentSlotData == null || _currentSlotData.IsEmpty || _registry == null)
            {
                if (_goldYieldField != null) _goldYieldField.value = "0";
                _itemYieldView?.Update(null);

                // Disable buttons
                if (_btnGetGold != null) _btnGetGold.SetEnabled(false);
                if (_btnGetItem != null) _btnGetItem.SetEnabled(false);
                return;
            }

            SalvageRecipeSO recipe = _registry.GetSalvageRecipeFor(_currentSlotData.HeldItem.BaseItem);

            if (recipe != null)
            {
                if (_goldYieldField != null) _goldYieldField.value = recipe.GoldYield.ToString();
                if (_btnGetGold != null) _btnGetGold.SetEnabled(recipe.GoldYield > 0);

                if (recipe.MaterialYields.Count > 0)
                {
                    // Just show the first yield for now
                    InventorySlot dummySlot = new InventorySlot();
                    dummySlot.SetItem(new ItemInstance(recipe.MaterialYields[0].Material), recipe.MaterialYields[0].Quantity);
                    _itemYieldView?.Update(dummySlot);
                    if (_btnGetItem != null) _btnGetItem.SetEnabled(true);
                }
                else
                {
                    _itemYieldView?.Update(null);
                    if (_btnGetItem != null) _btnGetItem.SetEnabled(false);
                }
            }
            else
            {
                if (_goldYieldField != null) _goldYieldField.value = "0";
                _itemYieldView?.Update(null);
                if (_btnGetGold != null) _btnGetGold.SetEnabled(false);
                if (_btnGetItem != null) _btnGetItem.SetEnabled(false);
            }
        }

        private void OnBtnGetGoldClicked(ClickEvent evt)
        {
            if (_currentSlotData != null && !_currentSlotData.IsEmpty)
            {
                _uiInventoryEvents?.OnRequestSalvage?.Invoke(_currentSlotData, SalvageType.Gold);
                ClearInputSlot();
            }
        }

        private void OnBtnGetItemClicked(ClickEvent evt)
        {
            if (_currentSlotData != null && !_currentSlotData.IsEmpty)
            {
                _uiInventoryEvents?.OnRequestSalvage?.Invoke(_currentSlotData, SalvageType.Material);
                ClearInputSlot();
            }
        }

        public override void Dispose()
        {
            if (_eventsBound && _uiInventoryEvents != null)
            {
                _uiInventoryEvents.OnItemClicked -= HandleItemClicked;
                _eventsBound = false;
            }
            base.Dispose();
        }
    }
}
