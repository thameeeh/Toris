using System.Collections.Generic;
using UnityEngine;
using OutlandHaven.Inventory;

namespace OutlandHaven.UIToolkit
{
    /// <summary>
    /// Acts as a central database for all crafting transformations and salvage recipes.
    /// </summary>
    [CreateAssetMenu(menuName = "UI/Crafting/Crafting Registry")]
    public class CraftingRegistrySO : ScriptableObject
    {
        [Tooltip("List of all transformative crafting recipes")]
        public List<CraftingRecipeSO> CraftingRecipes = new List<CraftingRecipeSO>();

        [Tooltip("List of all salvage recipes")]
        public List<SalvageRecipeSO> SalvageRecipes = new List<SalvageRecipeSO>();

        /// <summary>
        /// Attempts to find a salvage recipe for a given item.
        /// </summary>
        public SalvageRecipeSO GetSalvageRecipeFor(InventoryItemSO item)
        {
            foreach (var recipe in SalvageRecipes)
            {
                if (recipe != null && recipe.TargetItem == item)
                {
                    return recipe;
                }
            }
            return null;
        }

        /// <summary>
        /// Attempts to find a crafting recipe for a given base item.
        /// </summary>
        public CraftingRecipeSO GetCraftingRecipeFor(InventoryItemSO baseItem)
        {
            foreach (var recipe in CraftingRecipes)
            {
                if (recipe != null && recipe.BaseItemRequirement == baseItem)
                {
                    return recipe;
                }
            }
            return null;
        }
    }
}
