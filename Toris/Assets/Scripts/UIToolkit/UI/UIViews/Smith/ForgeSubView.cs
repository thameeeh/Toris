using System.Collections.Generic;
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
        private VisualElement _recipeListContainer;

        private InventorySlotView _slot1View;
        private InventorySlotView _slot2View;
        private InventorySlotView _resultSlotView;

        private InventorySlot _currentSlot1Data;
        private InventorySlot _currentSlot2Data;

        private InventorySlot _cachedSlot1;
        private InventorySlot _cachedSlot2;
        private CraftingRecipeSO _selectedRecipe;

        private Button _btnForgeItems;
        private List<VisualElement> _blueprintRows = new List<VisualElement>();

        private bool _eventsBound = false;

        private const int BaseRequirementPreviewQuantity = 1;
        private const int RecipeOutputPreviewQuantity = 1;
        private const string BlueprintRowClass = "forge-blueprint-row";
        private const string BlueprintRowSelectedClass = "forge-blueprint-row--selected";
        private const string BlueprintRowAvailableClass = "forge-blueprint-row--available";
        private const string BlueprintRowLockedClass = "forge-blueprint-row--locked";

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
            _recipeListContainer = m_TopElement.Q<VisualElement>("recipe-list-container");
            _btnForgeItems = m_TopElement.Q<Button>("btn-forge-items");

            if (_slot1Container != null)
            {
                TemplateContainer instance = _slotTemplate.Instantiate();
                instance.pickingMode = PickingMode.Ignore;
                instance.userData = "forge-slot-1";
                _slot1Container.Add(instance);
                _slot1View = new InventorySlotView(instance, null, _uiInventoryEvents);

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
                instance.pickingMode = PickingMode.Ignore;
                instance.userData = "forge-slot-2";
                _slot2Container.Add(instance);
                _slot2View = new InventorySlotView(instance, null, _uiInventoryEvents);

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
                instance.pickingMode = PickingMode.Ignore;
                _resultSlotContainer.Add(instance);
                _resultSlotView = new InventorySlotView(instance, null, _uiInventoryEvents);
            }
        }

        public override void Setup(object payload = null)
        {
            ClearSlot1();
            ClearSlot2();
            BuildBlueprintList();
            UpdateResultVisual();
        }

        public override void Show()
        {
            base.Show();
            _uiInventoryEvents?.OnInteractionContextChanged?.Invoke(InventoryInteractionContext.Forge);

            if (!_eventsBound && _uiInventoryEvents != null)
            {
                _uiInventoryEvents.OnItemClicked += HandleItemClicked;
                _uiInventoryEvents.OnRequestSelectForProcessing += HandleProxyDrop;
                _uiInventoryEvents.OnInventoryUpdated += HandleInventoryUpdated;
                _eventsBound = true;
            }

            if (_btnForgeItems != null)
            {
                _btnForgeItems.RegisterCallback<ClickEvent>(OnBtnForgeClicked);
            }
        }

        public override void Hide()
        {
            _uiInventoryEvents?.OnInteractionContextChanged?.Invoke(InventoryInteractionContext.Normal);
            base.Hide();

            if (_eventsBound && _uiInventoryEvents != null)
            {
                _uiInventoryEvents.OnItemClicked -= HandleItemClicked;
                _uiInventoryEvents.OnRequestSelectForProcessing -= HandleProxyDrop;
                _uiInventoryEvents.OnInventoryUpdated -= HandleInventoryUpdated;
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
            ClearSelectedBlueprint();

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
            ClearSelectedBlueprint();

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
            ClearSelectedBlueprint();
            _currentSlot1Data = null;
            _cachedSlot1 = null;
            _slot1View?.Update(null);
            UpdateResultVisual();
        }

        private void ClearSlot2()
        {
            ClearSelectedBlueprint();
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
            if (_selectedRecipe != null)
            {
                _uiInventoryEvents?.OnRequestCraftRecipe?.Invoke(_selectedRecipe);
                UpdateSelectedRecipePreview();
                RefreshBlueprintStates();
                return;
            }

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
                _uiInventoryEvents.OnInventoryUpdated -= HandleInventoryUpdated;
                _eventsBound = false;
            }
            base.Dispose();
        }

        private void BuildBlueprintList()
        {
            if (_recipeListContainer == null) return;

            _uiInventoryEvents?.OnItemTooltipHide?.Invoke();
            _recipeListContainer.Clear();
            _blueprintRows.Clear();

            if (_craftingManager?.Registry?.CraftingRecipes == null)
            {
                return;
            }

            foreach (var recipe in _craftingManager.Registry.CraftingRecipes)
            {
                if (recipe == null || recipe.OutputItem == null || recipe.BaseItemRequirement == null)
                {
                    continue;
                }

                CraftingRecipeSO rowRecipe = recipe;
                Button row = new Button();
                row.text = string.Empty;
                row.userData = rowRecipe;
                row.AddToClassList(BlueprintRowClass);
                row.clicked += () => SelectBlueprint(rowRecipe);
                row.RegisterCallback<PointerEnterEvent>(evt => ShowBlueprintTooltip(rowRecipe, evt.position));
                row.RegisterCallback<PointerMoveEvent>(evt => _uiInventoryEvents?.OnItemTooltipMove?.Invoke(evt.position));
                row.RegisterCallback<PointerLeaveEvent>(evt => _uiInventoryEvents?.OnItemTooltipHide?.Invoke());

                Image icon = new Image
                {
                    sprite = rowRecipe.OutputItem.Icon,
                    scaleMode = ScaleMode.ScaleToFit
                };
                icon.AddToClassList("forge-blueprint-icon");
                if (rowRecipe.OutputItem.Icon == null)
                {
                    icon.style.display = DisplayStyle.None;
                }

                VisualElement details = new VisualElement();
                details.AddToClassList("forge-blueprint-details");

                Label title = new Label(GetItemName(rowRecipe.OutputItem));
                title.AddToClassList("forge-blueprint-title");

                Label requirements = new Label(BuildRequirementText(rowRecipe));
                requirements.AddToClassList("forge-blueprint-requirements");

                details.Add(title);
                details.Add(requirements);
                row.Add(icon);
                row.Add(details);

                _recipeListContainer.Add(row);
                _blueprintRows.Add(row);
            }

            RefreshBlueprintStates();
        }

        private void SelectBlueprint(CraftingRecipeSO recipe)
        {
            if (recipe == null) return;
            _uiInventoryEvents?.OnItemTooltipHide?.Invoke();

            _selectedRecipe = recipe;
            _cachedSlot1 = null;
            _cachedSlot2 = null;
            UpdateSelectedRecipePreview();
            RefreshBlueprintStates();
        }

        private void ClearSelectedBlueprint()
        {
            if (_selectedRecipe == null) return;

            _selectedRecipe = null;
            RefreshBlueprintStates();
        }

        private void UpdateSelectedRecipePreview()
        {
            if (_selectedRecipe == null) return;

            _currentSlot1Data = CreatePreviewSlot(_selectedRecipe.BaseItemRequirement, BaseRequirementPreviewQuantity);
            _slot1View?.Update(_currentSlot1Data);

            if (TryGetFirstMaterialRequirement(_selectedRecipe, out CraftingMaterialRequirement firstMaterial))
            {
                _currentSlot2Data = CreatePreviewSlot(firstMaterial.Material, firstMaterial.Quantity);
                _slot2View?.Update(_currentSlot2Data);
            }
            else
            {
                _currentSlot2Data = null;
                _slot2View?.Update(null);
            }

            InventorySlot resultSlot = CreatePreviewSlot(_selectedRecipe.OutputItem, RecipeOutputPreviewQuantity);
            _resultSlotView?.Update(resultSlot);

            bool canCraft = _craftingManager != null && _craftingManager.CanCraftRecipe(_selectedRecipe);
            if (_btnForgeItems != null) _btnForgeItems.SetEnabled(canCraft);
        }

        private void RefreshBlueprintStates()
        {
            foreach (VisualElement row in _blueprintRows)
            {
                if (!(row.userData is CraftingRecipeSO recipe)) continue;

                bool canCraft = _craftingManager != null && _craftingManager.CanCraftRecipe(recipe);
                row.EnableInClassList(BlueprintRowSelectedClass, recipe == _selectedRecipe);
                row.EnableInClassList(BlueprintRowAvailableClass, canCraft);
                row.EnableInClassList(BlueprintRowLockedClass, !canCraft);
            }
        }

        private void HandleInventoryUpdated()
        {
            RefreshBlueprintStates();

            if (_selectedRecipe != null)
            {
                UpdateSelectedRecipePreview();
            }
            else
            {
                UpdateResultVisual();
            }
        }

        private void ShowBlueprintTooltip(CraftingRecipeSO recipe, Vector2 pointerPosition)
        {
            InventorySlot outputSlot = CreatePreviewSlot(recipe?.OutputItem, RecipeOutputPreviewQuantity);
            if (outputSlot == null)
                return;

            _uiInventoryEvents?.OnItemTooltipShow?.Invoke(outputSlot, pointerPosition);
        }

        private static InventorySlot CreatePreviewSlot(InventoryItemSO item, int quantity)
        {
            if (item == null || quantity <= 0) return null;

            InventorySlot slot = new InventorySlot();
            slot.SetItem(new ItemInstance(item), quantity);
            return slot;
        }

        private static bool TryGetFirstMaterialRequirement(CraftingRecipeSO recipe, out CraftingMaterialRequirement materialRequirement)
        {
            materialRequirement = default(CraftingMaterialRequirement);

            if (recipe?.MaterialRequirements == null) return false;

            foreach (CraftingMaterialRequirement requirement in recipe.MaterialRequirements)
            {
                if (requirement.Material != null && requirement.Quantity > 0)
                {
                    materialRequirement = requirement;
                    return true;
                }
            }

            return false;
        }

        private static string BuildRequirementText(CraftingRecipeSO recipe)
        {
            string text = GetItemName(recipe.BaseItemRequirement);

            if (recipe.MaterialRequirements != null)
            {
                foreach (CraftingMaterialRequirement requirement in recipe.MaterialRequirements)
                {
                    if (requirement.Material == null || requirement.Quantity <= 0) continue;

                    text += " + " + GetItemName(requirement.Material);
                    if (requirement.Quantity > 1)
                    {
                        text += " x" + requirement.Quantity;
                    }
                }
            }

            if (recipe.GoldCost > 0)
            {
                text += " | " + recipe.GoldCost + "g";
            }

            return text;
        }

        private static string GetItemName(InventoryItemSO item)
        {
            if (item == null || string.IsNullOrEmpty(item.ItemName))
            {
                return "Unknown Item";
            }

            return NicifyItemName(item.ItemName);
        }

        private static string NicifyItemName(string itemName)
        {
            char[] characters = itemName.Replace('_', ' ').ToLowerInvariant().ToCharArray();
            bool capitalizeNext = true;

            for (int i = 0; i < characters.Length; i++)
            {
                char current = characters[i];
                if (char.IsWhiteSpace(current) || current == '-')
                {
                    capitalizeNext = true;
                    continue;
                }

                if (capitalizeNext)
                {
                    characters[i] = char.ToUpperInvariant(current);
                    capitalizeNext = false;
                }
            }

            return new string(characters);
        }
    }
}
