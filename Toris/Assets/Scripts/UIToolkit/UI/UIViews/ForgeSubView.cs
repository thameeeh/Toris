using UnityEngine;
using UnityEngine.UIElements;
using OutlandHaven.Inventory;

namespace OutlandHaven.UIToolkit
{
    public class ForgeSubView : UIView
    {
        private VisualTreeAsset _slotTemplate;
        private UIInventoryEventsSO _uiInventoryEvents;
        private CraftingManagerSO _craftingManager;

        private VisualElement _slot1Container;
        private VisualElement _slot2Container;
        private VisualElement _resultSlotContainer;

        private InventorySlotView _slot1View;
        private InventorySlotView _slot2View;
        private InventorySlotView _resultSlotView;

        private InventorySlot _currentSlot1Data;
        private InventorySlot _currentSlot2Data;

        private InventorySlot _cachedSlot1;
        private InventorySlot _cachedSlot2;

        private Button _btnForgeItems;

        private bool _eventsBound = false;

        public ForgeSubView(VisualElement topElement, VisualTreeAsset slotTemplate, UIInventoryEventsSO uiInventoryEvents, CraftingManagerSO craftingManager)
            : base(topElement)
        {
            _slotTemplate = slotTemplate;
            _uiInventoryEvents = uiInventoryEvents;
            _craftingManager = craftingManager;
        }

        protected override void SetVisualElements()
        {
            _slot1Container = m_TopElement.Q<VisualElement>("forge-slot-1");
            _slot2Container = m_TopElement.Q<VisualElement>("forge-slot-2");
            _resultSlotContainer = m_TopElement.Q<VisualElement>("forge-result-slot");
            _btnForgeItems = m_TopElement.Q<Button>("btn-forge-items");

            if (_slot1Container != null)
            {
                TemplateContainer instance = _slotTemplate.Instantiate();
                instance.userData = "forge-slot-1";
                _slot1Container.Add(instance);
                _slot1View = new InventorySlotView(instance, null);

                _slot1View.OnLocalClicked += (slot) => _uiInventoryEvents.OnItemClicked?.Invoke(slot);
                _slot1View.OnLocalRightClicked += (slot) => _uiInventoryEvents.OnItemRightClicked?.Invoke(slot);
                _slot1View.OnLocalMoveItemRequested += (sourceContainer, sourceSlot, targetContainer, targetSlot) => _uiInventoryEvents.OnRequestMoveItem?.Invoke(sourceContainer, sourceSlot, targetContainer, targetSlot);
                _slot1View.OnLocalSelectForProcessingRequested += (slot, proxyID) => _uiInventoryEvents.OnRequestSelectForProcessing?.Invoke(slot, proxyID);

                instance.RegisterCallback<MouseUpEvent>(evt =>
                {
                    if (evt.button == 0) // Left click
                    {
                        ClearSlot1();
                    }
                });
            }

            if (_slot2Container != null)
            {
                TemplateContainer instance = _slotTemplate.Instantiate();
                instance.userData = "forge-slot-2";
                _slot2Container.Add(instance);
                _slot2View = new InventorySlotView(instance, null);

                _slot2View.OnLocalClicked += (slot) => _uiInventoryEvents.OnItemClicked?.Invoke(slot);
                _slot2View.OnLocalRightClicked += (slot) => _uiInventoryEvents.OnItemRightClicked?.Invoke(slot);
                _slot2View.OnLocalMoveItemRequested += (sourceContainer, sourceSlot, targetContainer, targetSlot) => _uiInventoryEvents.OnRequestMoveItem?.Invoke(sourceContainer, sourceSlot, targetContainer, targetSlot);
                _slot2View.OnLocalSelectForProcessingRequested += (slot, proxyID) => _uiInventoryEvents.OnRequestSelectForProcessing?.Invoke(slot, proxyID);

                instance.RegisterCallback<MouseUpEvent>(evt =>
                {
                    if (evt.button == 0) // Left click
                    {
                        ClearSlot2();
                    }
                });
            }

            if (_resultSlotContainer != null)
            {
                TemplateContainer instance = _slotTemplate.Instantiate();
                _resultSlotContainer.Add(instance);
                _resultSlotView = new InventorySlotView(instance, null);

                _resultSlotView.OnLocalClicked += (slot) => _uiInventoryEvents.OnItemClicked?.Invoke(slot);
                _resultSlotView.OnLocalRightClicked += (slot) => _uiInventoryEvents.OnItemRightClicked?.Invoke(slot);
                _resultSlotView.OnLocalMoveItemRequested += (sourceContainer, sourceSlot, targetContainer, targetSlot) => _uiInventoryEvents.OnRequestMoveItem?.Invoke(sourceContainer, sourceSlot, targetContainer, targetSlot);
                _resultSlotView.OnLocalSelectForProcessingRequested += (slot, proxyID) => _uiInventoryEvents.OnRequestSelectForProcessing?.Invoke(slot, proxyID);
            }
        }

        public override void Setup(object payload = null)
        {
            ClearSlot1();
            ClearSlot2();
            UpdateResultVisual();
        }

        public override void Show()
        {
            base.Show();
            if (!_eventsBound && _uiInventoryEvents != null)
            {
                _uiInventoryEvents.OnItemClicked += HandleItemClicked;
                _uiInventoryEvents.OnRequestSelectForProcessing += HandleProxyDrop;
                _eventsBound = true;
            }

            if (_btnForgeItems != null)
            {
                _btnForgeItems.RegisterCallback<ClickEvent>(OnBtnForgeClicked);
            }
        }

        public override void Hide()
        {
            base.Hide();
            if (_eventsBound && _uiInventoryEvents != null)
            {
                _uiInventoryEvents.OnItemClicked -= HandleItemClicked;
                _uiInventoryEvents.OnRequestSelectForProcessing -= HandleProxyDrop;
                _eventsBound = false;
            }

            if (_btnForgeItems != null)
            {
                _btnForgeItems.UnregisterCallback<ClickEvent>(OnBtnForgeClicked);
            }
        }

        private void HandleProxyDrop(InventorySlot sourceSlot, string slotID)
        {
            if (sourceSlot == null || sourceSlot.IsEmpty) return;

            if (slotID == "forge-slot-1")
            {
                InventorySlot proxySlot = new InventorySlot();
                proxySlot.SetItem(new ItemInstance(sourceSlot.HeldItem.BaseItem), sourceSlot.Count); // Full stack

                _currentSlot1Data = proxySlot;
                _cachedSlot1 = sourceSlot;
                _slot1View?.Update(proxySlot);
                UpdateResultVisual();
            }
            else if (slotID == "forge-slot-2")
            {
                InventorySlot proxySlot = new InventorySlot();
                proxySlot.SetItem(new ItemInstance(sourceSlot.HeldItem.BaseItem), sourceSlot.Count); // Full stack

                _currentSlot2Data = proxySlot;
                _cachedSlot2 = sourceSlot;
                _slot2View?.Update(proxySlot);
                UpdateResultVisual();
            }
        }

        private void HandleItemClicked(InventorySlot slot)
        {
            if (slot == null || slot.IsEmpty) return;

            InventorySlot proxySlot = new InventorySlot();
            proxySlot.SetItem(new ItemInstance(slot.HeldItem.BaseItem), 1);

            if (_currentSlot1Data == null)
            {
                _currentSlot1Data = proxySlot;
                _cachedSlot1 = slot;
                _slot1View?.Update(proxySlot);
            }
            else if (_currentSlot2Data == null)
            {
                _currentSlot2Data = proxySlot;
                _cachedSlot2 = slot;
                _slot2View?.Update(proxySlot);
            }

            UpdateResultVisual();
        }

        private void ClearSlot1()
        {
            _currentSlot1Data = null;
            _cachedSlot1 = null;
            _slot1View?.Update(null);
            UpdateResultVisual();
        }

        private void ClearSlot2()
        {
            _currentSlot2Data = null;
            _cachedSlot2 = null;
            _slot2View?.Update(null);
            UpdateResultVisual();
        }

        private void UpdateResultVisual()
        {
            if (_currentSlot1Data == null || _currentSlot2Data == null || _craftingManager == null)
            {
                _resultSlotView?.Update(null);
                if (_btnForgeItems != null) _btnForgeItems.SetEnabled(false);
                return;
            }

            // Check if there's a valid recipe
            CraftingRecipeSO recipe = _craftingManager.GetMatchingRecipe(_currentSlot1Data.HeldItem.BaseItem, _currentSlot2Data.HeldItem.BaseItem);

            if (recipe != null)
            {
                int slot1Req = 1;
                int slot2Req = 1;

                bool canForge = _craftingManager.CanForge(recipe, _currentSlot1Data, _currentSlot2Data, out slot1Req, out slot2Req);

                _currentSlot1Data.Count = slot1Req;
                _slot1View?.Update(_currentSlot1Data);

                _currentSlot2Data.Count = slot2Req;
                _slot2View?.Update(_currentSlot2Data);

                InventorySlot dummySlot = new InventorySlot();
                dummySlot.SetItem(new ItemInstance(recipe.OutputItem), 1);
                _resultSlotView?.Update(dummySlot);

                if (_btnForgeItems != null) _btnForgeItems.SetEnabled(canForge);
            }
            else
            {
                _resultSlotView?.Update(null);
                if (_btnForgeItems != null) _btnForgeItems.SetEnabled(false);
            }
        }

        private void OnBtnForgeClicked(ClickEvent evt)
        {
            if (_currentSlot1Data != null && _currentSlot2Data != null && _cachedSlot1 != null && _cachedSlot2 != null)
            {
                _uiInventoryEvents?.OnRequestForge?.Invoke(_cachedSlot1, _cachedSlot2);
                ClearSlot1();
                ClearSlot2();
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
