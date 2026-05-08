using UnityEngine;
using OutlandHaven.UIToolkit;
using System;

namespace OutlandHaven.Inventory
{

    [System.Serializable]
    public abstract class ItemComponent
    {
        // By default, components don't have a runtime state (e.g., a simple IconComponent).
        // Override this only in components that need dynamic tracking.
        public virtual ItemComponentState CreateInitialState()
        {
            return null;
        }

        public virtual string GetStackingValidationMessage(InventoryItemSO owner, int maxStackSize)
        {
            return null;
        }

        /// <summary>
        /// Allows a component to impose a hard limit on the Item's MaxStackSize.
        /// Returns int.MaxValue by default (no limit).
        /// </summary>
        public virtual int GetMaxStackSizeLimit()
        {
            return int.MaxValue;
        }
    }

}
