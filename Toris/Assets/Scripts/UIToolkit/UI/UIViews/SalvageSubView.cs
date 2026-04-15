using UnityEngine;
using UnityEngine.UIElements;
using OutlandHaven.Inventory;

namespace OutlandHaven.UIToolkit
{
    public class SalvageSubView : UIView
    {
        private VisualTreeAsset _slotTemplate;
        private UIInventoryEventsSO _uiInventoryEvents;
        private SalvageManagerSO _salvageManager;

        private VisualElement _inputSlotContainer;
        private InventorySlotView _inputSlotView;
        private InventorySlot _currentSlotData;
        private InventorySlot _cachedSourceSlot; // Cache the source slot for validation

        private TextField _goldYieldField;
        private VisualElement _itemYieldContainer;
        private InventorySlotView _itemYieldView;

        private Button _btnGetGold;
        private Button _btnGetItem;

        private bool _eventsBound = false;

        public SalvageSubView(VisualElement topElement, VisualTreeAsset slotTemplate, UIInventoryEventsSO uiInventoryEvents, SalvageManagerSO salvageManager)
            : base(topElement)
        {
            _slotTemplate = slotTemplate;
            _uiInventoryEvents = uiInventoryEvents;
            _salvageManager = salvageManager;
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
                instance.userData = "salvage-input"; // Set proxy ID
                _inputSlotContainer.Add(instance);
                _inputSlotView = new InventorySlotView(instance, null);

                _inputSlotView.OnLocalClicked += (slot) => _uiInventoryEvents.OnItemClicked?.Invoke(slot);
                _inputSlotView.OnLocalRightClicked += HandleItemRightClicked;
                _inputSlotView.OnLocalMoveItemRequested += (sourceContainer, sourceSlot, targetContainer, targetSlot, amountToMove) => _uiInventoryEvents.OnRequestMoveItem?.Invoke(sourceContainer, sourceSlot, targetContainer, targetSlot, sourceSlot.Count);
                _inputSlotView.OnLocalSelectForProcessingRequested += (slot, proxyID) => _uiInventoryEvents.OnRequestSelectForProcessing?.Invoke(slot, proxyID);

                _inputSlotView.OnLocalDragStarted += (sprite, pos, size) => _uiInventoryEvents.OnGlobalDragStarted?.Invoke(sprite, pos, size);
                _inputSlotView.OnLocalDragUpdated += (pos) => _uiInventoryEvents.OnGlobalDragUpdated?.Invoke(pos);
                _inputSlotView.OnLocalDragStopped += () => _uiInventoryEvents.OnGlobalDragStopped?.Invoke();

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
                _itemYieldView = new InventorySlotView(instance, null);

                _itemYieldView.OnLocalClicked += (slot) => _uiInventoryEvents.OnItemClicked?.Invoke(slot);
                _itemYieldView.OnLocalRightClicked += HandleItemRightClicked;
                _itemYieldView.OnLocalMoveItemRequested += (sourceContainer, sourceSlot, targetContainer, targetSlot, amountToMove) => _uiInventoryEvents.OnRequestMoveItem?.Invoke(sourceContainer, sourceSlot, targetContainer, targetSlot, sourceSlot.Count);
                _itemYieldView.OnLocalSelectForProcessingRequested += (slot, proxyID) => _uiInventoryEvents.OnRequestSelectForProcessing?.Invoke(slot, proxyID);

                _itemYieldView.OnLocalDragStarted += (sprite, pos, size) => _uiInventoryEvents.OnGlobalDragStarted?.Invoke(sprite, pos, size);
                _itemYieldView.OnLocalDragUpdated += (pos) => _uiInventoryEvents.OnGlobalDragUpdated?.Invoke(pos);
                _itemYieldView.OnLocalDragStopped += () => _uiInventoryEvents.OnGlobalDragStopped?.Invoke();
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
            _uiInventoryEvents?.OnInteractionContextChanged?.Invoke(InventoryInteractionContext.Salvage);
            if (!_eventsBound && _uiInventoryEvents != null)
            {
                _uiInventoryEvents.OnItemClicked += HandleItemClicked;

                _uiInventoryEvents.OnRequestSelectForProcessing += HandleProxyDrop;
                _eventsBound = true;
            }

            _btnGetGold?.RegisterCallback<ClickEvent>(OnBtnGetGoldClicked);
            _btnGetItem?.RegisterCallback<ClickEvent>(OnBtnGetItemClicked);
        }

        public override void Hide()
        {
            _uiInventoryEvents?.OnInteractionContextChanged?.Invoke(InventoryInteractionContext.Normal);
            base.Hide();
            if (_eventsBound && _uiInventoryEvents != null)
            {
                _uiInventoryEvents.OnItemClicked -= HandleItemClicked;

                _uiInventoryEvents.OnRequestSelectForProcessing -= HandleProxyDrop;
                _eventsBound = false;
            }

            _btnGetGold?.UnregisterCallback<ClickEvent>(OnBtnGetGoldClicked);
            _btnGetItem?.UnregisterCallback<ClickEvent>(OnBtnGetItemClicked);
        }

        private void HandleProxyDrop(InventorySlot sourceSlot, string slotID)
        {
            if (slotID == "salvage-input")
            {
                if (sourceSlot == null || sourceSlot.IsEmpty) return;

                InventorySlot proxySlot = new InventorySlot();
                proxySlot.SetItem(new ItemInstance(sourceSlot.HeldItem.BaseItem), sourceSlot.Count); // Full stack

                _currentSlotData = proxySlot;
                _cachedSourceSlot = sourceSlot;

                _inputSlotView?.Update(proxySlot);
                UpdateYieldVisuals();
            }
        }

        private void HandleItemClicked(InventorySlot slot)
        {
            if (slot == null || slot.IsEmpty) return;

            // Set the clicked item into a proxy slot visually
            InventorySlot proxySlot = new InventorySlot();
            proxySlot.SetItem(new ItemInstance(slot.HeldItem.BaseItem), 1);

            _currentSlotData = proxySlot;
            _cachedSourceSlot = slot;

            // To display correctly in the visual slot view which might expect an InventorySlot
            _inputSlotView?.Update(proxySlot);

            UpdateYieldVisuals();
        }

        private void HandleItemRightClicked(InventorySlot slot)
        {
            if (slot == null || slot.IsEmpty) return;

            // If we right clicked the same item that's in the salvage proxy slot (or a dummy),
            // or if we right clicked the proxy slot itself (if that's ever possible)
            if (_cachedSourceSlot == slot)
            {
                ClearInputSlot();
                return;
            }

            // Otherwise, set it as the proxy input slot if it's salvageable
            if (_salvageManager != null && _salvageManager.CanSalvage(slot.HeldItem.BaseItem))
            {
                HandleItemClicked(slot);
            }
        }

        private void ClearInputSlot()
        {
            _currentSlotData = null;
            _cachedSourceSlot = null;
            _inputSlotView?.Update(null);
            UpdateYieldVisuals();
        }

        private void UpdateYieldVisuals()
        {
            if (_currentSlotData == null || _currentSlotData.IsEmpty || _salvageManager == null)
            {
                if (_goldYieldField != null) _goldYieldField.value = "0";
                _itemYieldView?.Update(null);

                // Disable buttons
                if (_btnGetGold != null) _btnGetGold.SetEnabled(false);
                if (_btnGetItem != null) _btnGetItem.SetEnabled(false);
                return;
            }

            SalvageRecipeSO recipe = _salvageManager.Registry.GetSalvageRecipeFor(_currentSlotData.HeldItem.BaseItem);

            if (recipe != null)
            {
                // Check if player actually has the item to salvage
                bool canSalvage = _salvageManager.CanSalvage(_currentSlotData.HeldItem.BaseItem);

                if (_goldYieldField != null) _goldYieldField.value = recipe.GoldYield.ToString();
                if (_btnGetGold != null) _btnGetGold.SetEnabled(canSalvage && recipe.GoldYield > 0);

                if (recipe.MaterialYields.Count > 0)
                {
                    // Just show the first yield for now
                    InventorySlot dummySlot = new InventorySlot();
                    dummySlot.SetItem(new ItemInstance(recipe.MaterialYields[0].Material), recipe.MaterialYields[0].Quantity);
                    _itemYieldView?.Update(dummySlot);
                    if (_btnGetItem != null) _btnGetItem.SetEnabled(canSalvage);
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
            if (_currentSlotData != null && !_currentSlotData.IsEmpty && _cachedSourceSlot != null)
            {
                _uiInventoryEvents?.OnRequestSalvage?.Invoke(_cachedSourceSlot, SalvageType.Gold);
                ClearInputSlot();
            }
        }

        private void OnBtnGetItemClicked(ClickEvent evt)
        {
            if (_currentSlotData != null && !_currentSlotData.IsEmpty && _cachedSourceSlot != null)
            {
                _uiInventoryEvents?.OnRequestSalvage?.Invoke(_cachedSourceSlot, SalvageType.Material);
                ClearInputSlot();
            }
        }

        public override void Dispose()
        {
            if (_eventsBound && _uiInventoryEvents != null)
            {
                _uiInventoryEvents.OnItemClicked -= HandleItemClicked;

                _uiInventoryEvents.OnRequestSelectForProcessing -= HandleProxyDrop;
                _eventsBound = false;
            }
            base.Dispose();
        }
    }
}
