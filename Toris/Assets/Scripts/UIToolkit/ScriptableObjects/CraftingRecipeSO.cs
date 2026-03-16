using System.Collections.Generic;
using UnityEngine;

namespace OutlandHaven.UIToolkit
{
    /// <summary>
    /// Recipe for transformative crafting (e.g., combining items into entirely new items).
    /// </summary>
    [CreateAssetMenu(menuName = "UI/Crafting/Crafting Recipe")]
    public class CraftingRecipeSO : ScriptableObject
    {
        [Header("Inputs")]
        [Tooltip("The base item required for this transformation")]
        public InventoryItemSO BaseItemRequirement;

        [Tooltip("Other items required to craft this recipe")]
        public List<CraftingMaterialRequirement> MaterialRequirements = new List<CraftingMaterialRequirement>();

        [Tooltip("Gold required to craft this recipe")]
        public int GoldCost = 0;

        [Header("Outputs")]
        [Tooltip("The resulting new item from this transformation")]
        public InventoryItemSO OutputItem;
    }

    [System.Serializable]
    public struct CraftingMaterialRequirement
    {
        public InventoryItemSO Material;
        public int Quantity;
    }
}
