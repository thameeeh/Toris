using System.Collections.Generic;
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

        [Header("Modular Behaviours")]
        [SerializeReference]
        public List<ItemComponent> Components = new List<ItemComponent>();

        public T GetComponent<T>() where T : ItemComponent
        {
            foreach (var component in Components)
            {
                if (component is T typedComponent)
                    return typedComponent;
            }
            return null;
        }
    }
}