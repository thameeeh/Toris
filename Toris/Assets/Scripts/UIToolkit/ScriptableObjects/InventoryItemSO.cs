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
        [Tooltip("How many identical items can share one inventory slot. This is inventory behavior only; loot tables control how many items drop.")]
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
            MaxStackSize = Mathf.Max(1, MaxStackSize);

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

                if (TryGetStackingValidationMessage(out string validationMessage))
                {
                    Debug.LogError(validationMessage, this);
                }
            }
        }
#endif

        public bool TryGetStackingValidationMessage(out string validationMessage)
        {
            validationMessage = null;

            if (Components == null)
                return false;

            for (int i = 0; i < Components.Count; i++)
            {
                ItemComponent component = Components[i];
                if (component == null)
                    continue;

                string componentMessage = component.GetStackingValidationMessage(this, MaxStackSize);
                if (string.IsNullOrEmpty(componentMessage))
                    continue;

                validationMessage = componentMessage;
                return true;
            }

            return false;
        }
    }
}
