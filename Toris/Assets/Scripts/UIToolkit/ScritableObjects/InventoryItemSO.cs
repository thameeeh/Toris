using UnityEngine;

namespace OutlandHaven.UIToolkit
{
    [CreateAssetMenu(menuName = "UI/Inventory/Item")]
    public class InventoryItemSO : ScriptableObject
    {
        public string ItemName;
        [TextArea] public string Description;
        public Sprite Icon;
        public int MaxStackSize = 99;
        public int GoldValue = 10;

        //add types later (Weapon, Potion, etc.)
    }
}