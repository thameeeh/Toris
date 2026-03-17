using System.Collections.Generic;
using UnityEngine;
using OutlandHaven.Inventory;

namespace OutlandHaven.UIToolkit
{
    /// <summary>
    /// Recipe defining what materials an item yields when destroyed.
    /// </summary>
    [CreateAssetMenu(menuName = "UI/Crafting/Salvage Recipe")]
    public class SalvageRecipeSO : ScriptableObject
    {
        [Header("Input")]
        [Tooltip("The item to be salvaged")]
        public InventoryItemSO TargetItem;

        [Header("Outputs")]
        [Tooltip("Gold received from salvaging")]
        public int GoldYield = 0;

        [Tooltip("Items received from salvaging (for future implementation)")]
        public List<CraftingMaterialRequirement> MaterialYields = new List<CraftingMaterialRequirement>();
    }
}
