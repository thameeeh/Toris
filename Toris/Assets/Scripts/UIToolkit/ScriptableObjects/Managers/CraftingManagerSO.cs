using System.Collections.Generic;
using UnityEngine;
using OutlandHaven.Inventory;

namespace OutlandHaven.UIToolkit
{
    [CreateAssetMenu(menuName = "UI/Scriptable Objects/CraftingManagerSO")]
    public class CraftingManagerSO : ScriptableObject
    {
        [Header("Dependencies")]
        public GameSessionSO SessionData;
        public PlayerProgressionAnchorSO PlayerAnchor;
        public UIInventoryEventsSO InventoryEvents;
        public CraftingRegistrySO Registry;

        private const int BaseItemRequirementQuantity = 1;
        private const int RecipeOutputQuantity = 1;

        public void Initialize()
        {
            Cleanup();
            if (InventoryEvents != null)
            {
                InventoryEvents.OnRequestForge += HandleRequestForge;
                InventoryEvents.OnRequestCraftRecipe += HandleRequestCraftRecipe;
            }
        }

        public void Cleanup()
        {
            if (InventoryEvents != null)
            {
                InventoryEvents.OnRequestForge -= HandleRequestForge;
                InventoryEvents.OnRequestCraftRecipe -= HandleRequestCraftRecipe;
            }
        }

        public bool CanForge(CraftingRecipeSO recipe, InventorySlot slot1, InventorySlot slot2, out int slot1Req, out int slot2Req)
        {
            slot1Req = 0;
            slot2Req = 0;

            if (recipe == null || slot1 == null || slot1.IsEmpty || slot2 == null || slot2.IsEmpty) return false;

            InventoryItemSO item1Type = slot1.HeldItem.BaseItem;
            InventoryItemSO item2Type = slot2.HeldItem.BaseItem;

            // Determine required quantities based on the recipe before early returns.
            slot1Req = BaseItemRequirementQuantity;
            slot2Req = BaseItemRequirementQuantity;

            if (recipe.BaseItemRequirement == item1Type)
            {
                if (!TryGetMaterialQuantity(recipe, item2Type, out slot2Req)) return false;
            }
            else if (recipe.BaseItemRequirement == item2Type)
            {
                if (!TryGetMaterialQuantity(recipe, item1Type, out slot1Req)) return false;
            }
            else
            {
                return false;
            }

            return CanCraftRecipe(recipe);
        }

        public bool CanCraftRecipe(CraftingRecipeSO recipe)
        {
            if (!CanUseCraftingServices()) return false;
            if (!TryBuildRequirementMap(recipe, out Dictionary<InventoryItemSO, int> requirements)) return false;
            if (PlayerAnchor.Instance.CurrentGold < recipe.GoldCost) return false;

            foreach (var requirement in requirements)
            {
                if (GetInventoryCount(requirement.Key) < requirement.Value)
                {
                    return false;
                }
            }

            return true;
        }

        private void HandleRequestForge(InventorySlot slot1, InventorySlot slot2)
        {
            if (slot1 == null || slot1.IsEmpty || slot2 == null || slot2.IsEmpty) return;
            if (Registry == null) return;

            // Cache items before doing anything that could invalidate references
            InventoryItemSO item1Type = slot1.HeldItem.BaseItem;
            InventoryItemSO item2Type = slot2.HeldItem.BaseItem;

            // Find a matching recipe
            CraftingRecipeSO recipe = GetMatchingRecipe(item1Type, item2Type);
            if (recipe == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("Forge failed: No matching recipe found.");
#endif
                return;
            }

            TryCraftRecipe(recipe);
        }

        private void HandleRequestCraftRecipe(CraftingRecipeSO recipe)
        {
            TryCraftRecipe(recipe);
        }

        private bool TryCraftRecipe(CraftingRecipeSO recipe)
        {
            if (!CanUseCraftingServices()) return false;
            if (!TryBuildRequirementMap(recipe, out Dictionary<InventoryItemSO, int> requirements)) return false;

            if (PlayerAnchor.Instance.CurrentGold < recipe.GoldCost)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"Forge failed: Not enough gold. Need {recipe.GoldCost}, have {PlayerAnchor.Instance.CurrentGold}.");
#endif
                return false;
            }

            foreach (var requirement in requirements)
            {
                if (GetInventoryCount(requirement.Key) < requirement.Value)
                {
#if UNITY_EDITOR
                    Debug.LogWarning($"Forge failed: Not enough {requirement.Key.ItemName}. Need {requirement.Value}.");
#endif
                    return false;
                }
            }

            List<KeyValuePair<InventoryItemSO, int>> removedItems = new List<KeyValuePair<InventoryItemSO, int>>();
            foreach (var requirement in requirements)
            {
                bool removed = SessionData.PlayerInventory.RemoveItem(new ItemInstance(requirement.Key), requirement.Value);
                if (!removed)
                {
                    RollbackRemovedItems(removedItems);
#if UNITY_EDITOR
                    Debug.LogWarning($"Forge failed: Could not remove {requirement.Key.ItemName}.");
#endif
                    return false;
                }

                removedItems.Add(requirement);
            }

            bool added = SessionData.PlayerInventory.AddItem(new ItemInstance(recipe.OutputItem), RecipeOutputQuantity);
            if (!added)
            {
                RollbackRemovedItems(removedItems);
#if UNITY_EDITOR
                Debug.LogWarning("Forge failed: Inventory full. Refunded ingredients.");
#endif
                return false;
            }

            PlayerAnchor.Instance.TrySpendGold(recipe.GoldCost);
            InventoryEvents?.OnInventoryUpdated?.Invoke();
#if UNITY_EDITOR
            Debug.Log($"Forged {recipe.OutputItem.ItemName} successfully.");
#endif
            return true;
        }

        public CraftingRecipeSO GetMatchingRecipe(InventoryItemSO itemA, InventoryItemSO itemB)
        {
            if (Registry == null || Registry.CraftingRecipes == null) return null;

            foreach (var recipe in Registry.CraftingRecipes)
            {
                if (recipe == null) continue;

                // Check if itemA is the base and itemB is the material
                if (recipe.BaseItemRequirement == itemA && RecipeContainsMaterial(recipe, itemB))
                {
                    return recipe;
                }

                // Check if itemB is the base and itemA is the material
                if (recipe.BaseItemRequirement == itemB && RecipeContainsMaterial(recipe, itemA))
                {
                    return recipe;
                }
            }
            return null;
        }

        private bool CanUseCraftingServices()
        {
            return SessionData != null
                   && SessionData.PlayerInventory != null
                   && PlayerAnchor != null
                   && PlayerAnchor.IsReady;
        }

        private bool TryBuildRequirementMap(CraftingRecipeSO recipe, out Dictionary<InventoryItemSO, int> requirements)
        {
            requirements = new Dictionary<InventoryItemSO, int>();

            if (recipe == null || recipe.BaseItemRequirement == null || recipe.OutputItem == null)
            {
                return false;
            }

            AddRequirement(requirements, recipe.BaseItemRequirement, BaseItemRequirementQuantity);

            if (recipe.MaterialRequirements != null)
            {
                foreach (var materialRequirement in recipe.MaterialRequirements)
                {
                    if (materialRequirement.Material == null || materialRequirement.Quantity <= 0)
                    {
                        return false;
                    }

                    AddRequirement(requirements, materialRequirement.Material, materialRequirement.Quantity);
                }
            }

            return requirements.Count > 0;
        }

        private static void AddRequirement(Dictionary<InventoryItemSO, int> requirements, InventoryItemSO item, int quantity)
        {
            if (requirements.TryGetValue(item, out int existingQuantity))
            {
                requirements[item] = existingQuantity + quantity;
            }
            else
            {
                requirements.Add(item, quantity);
            }
        }

        private static bool RecipeContainsMaterial(CraftingRecipeSO recipe, InventoryItemSO item)
        {
            return TryGetMaterialQuantity(recipe, item, out _);
        }

        private static bool TryGetMaterialQuantity(CraftingRecipeSO recipe, InventoryItemSO item, out int quantity)
        {
            quantity = 0;

            if (recipe?.MaterialRequirements == null || item == null) return false;

            foreach (CraftingMaterialRequirement requirement in recipe.MaterialRequirements)
            {
                if (requirement.Material == item)
                {
                    quantity = requirement.Quantity;
                    return true;
                }
            }

            return false;
        }

        private int GetInventoryCount(InventoryItemSO itemType)
        {
            if (itemType == null || SessionData?.PlayerInventory?.LiveSlots == null) return 0;

            int count = 0;
            ItemInstance matchingItem = new ItemInstance(itemType);
            foreach (var slot in SessionData.PlayerInventory.LiveSlots)
            {
                if (!slot.IsEmpty && slot.HeldItem.IsStackableWith(matchingItem))
                {
                    count += slot.Count;
                }
            }

            return count;
        }

        private void RollbackRemovedItems(List<KeyValuePair<InventoryItemSO, int>> removedItems)
        {
            foreach (var removedItem in removedItems)
            {
                SessionData.PlayerInventory.AddItem(new ItemInstance(removedItem.Key), removedItem.Value);
            }
        }
    }
}
