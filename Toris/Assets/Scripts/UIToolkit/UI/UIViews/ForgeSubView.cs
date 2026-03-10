using UnityEngine;
using UnityEngine.UIElements;

namespace OutlandHaven.UIToolkit
{
    public class ForgeSubView : UIView
    {
        private VisualTreeAsset _slotTemplate;
        private UIInventoryEventsSO _uiInventoryEvents;
        private CraftingRegistrySO _registry;

        private VisualElement _slot1Container;
        private VisualElement _slot2Container;
        private VisualElement _resultSlotContainer;

        private InventorySlotView _slot1View;
        private InventorySlotView _slot2View;
        private InventorySlotView _resultSlotView;

        private InventorySlot _currentSlot1Data;
        private InventorySlot _currentSlot2Data;

        private Button _btnForgeItems;

        private bool _eventsBound = false;

        public ForgeSubView(VisualElement topElement, VisualTreeAsset slotTemplate, UIInventoryEventsSO uiInventoryEvents, CraftingRegistrySO registry)
            : base(topElement)
        {
            _slotTemplate = slotTemplate;
            _uiInventoryEvents = uiInventoryEvents;
            _registry = registry;
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
                _slot1Container.Add(instance);
                _slot1View = new InventorySlotView(instance);

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
                _slot2Container.Add(instance);
                _slot2View = new InventorySlotView(instance);

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
                _resultSlotView = new InventorySlotView(instance);
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
                _eventsBound = false;
            }

            if (_btnForgeItems != null)
            {
                _btnForgeItems.UnregisterCallback<ClickEvent>(OnBtnForgeClicked);
            }
        }

        private void HandleItemClicked(InventorySlot slot)
        {
            if (_currentSlot1Data == null)
            {
                _currentSlot1Data = slot;
                _slot1View?.Update(slot);
            }
            else if (_currentSlot2Data == null)
            {
                _currentSlot2Data = slot;
                _slot2View?.Update(slot);
            }

            UpdateResultVisual();
        }

        private void ClearSlot1()
        {
            _currentSlot1Data = null;
            _slot1View?.Update(null);
            UpdateResultVisual();
        }

        private void ClearSlot2()
        {
            _currentSlot2Data = null;
            _slot2View?.Update(null);
            UpdateResultVisual();
        }

        private void UpdateResultVisual()
        {
            if (_currentSlot1Data == null || _currentSlot2Data == null || _registry == null)
            {
                _resultSlotView?.Update(null);
                if (_btnForgeItems != null) _btnForgeItems.SetEnabled(false);
                return;
            }

            // Check if there's a valid recipe
            CraftingRecipeSO recipe = GetMatchingRecipe(_currentSlot1Data.HeldItem.BaseItem, _currentSlot2Data.HeldItem.BaseItem);

            if (recipe != null)
            {
                InventorySlot dummySlot = new InventorySlot();
                dummySlot.SetItem(new ItemInstance(recipe.OutputItem), 1);
                _resultSlotView?.Update(dummySlot);

                if (_btnForgeItems != null) _btnForgeItems.SetEnabled(true);
            }
            else
            {
                _resultSlotView?.Update(null);
                if (_btnForgeItems != null) _btnForgeItems.SetEnabled(false);
            }
        }

        private void OnBtnForgeClicked(ClickEvent evt)
        {
            if (_currentSlot1Data != null && _currentSlot2Data != null)
            {
                _uiInventoryEvents?.OnRequestForge?.Invoke(_currentSlot1Data, _currentSlot2Data);
                ClearSlot1();
                ClearSlot2();
            }
        }

        private CraftingRecipeSO GetMatchingRecipe(InventoryItemSO itemA, InventoryItemSO itemB)
        {
            foreach (var recipe in _registry.CraftingRecipes)
            {
                if (recipe == null) continue;

                if (recipe.BaseItemRequirement == itemA &&
                    recipe.MaterialRequirements.Exists(m => m.Material == itemB))
                {
                    return recipe;
                }

                if (recipe.BaseItemRequirement == itemB &&
                    recipe.MaterialRequirements.Exists(m => m.Material == itemA))
                {
                    return recipe;
                }
            }
            return null;
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
