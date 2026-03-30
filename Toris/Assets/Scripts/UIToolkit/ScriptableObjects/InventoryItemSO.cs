using System.Collections.Generic;
using UnityEngine;


namespace OutlandHaven.Inventory
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
            if (Components == null) return null;

            foreach (var component in Components)
            {
                if (component is T typedComponent)
                    return typedComponent;
            }
            return null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // We iterate backwards when removing elements from a list 
            // to prevent index shifting bugs.
            if (Components != null)
            {
                for (int i = Components.Count - 1; i >= 0; i--)
                {
                    if (Components[i] == null)
                    {
                        Components.RemoveAt(i);
                        // Optional: Log to let you know it cleaned up a serialization glitch
                        Debug.LogWarning($"Cleaned up a null component in {ItemName}"); 
                    }
                }
            }
        }
#endif
    }
}