using System;
using System.Collections.Generic;
using UnityEngine;

namespace OutlandHaven.Inventory
{
    /// <summary>
    /// Wrapper class holding the runtime state of an item.
    /// </summary>
    [Serializable]
    public class ItemInstance
    {
        public string InstanceID; // Mandatory for saving/loading individual items
        public InventoryItemSO BaseItem;

        // Holds the runtime data (e.g., DurabilityState, ConsumableState)
        [SerializeReference]
        public List<ItemComponentState> States = new List<ItemComponentState>();

        // Default constructor for serialization
        public ItemInstance()
        {
            InstanceID = Guid.NewGuid().ToString();
        }

        public ItemInstance(InventoryItemSO baseItem)
        {
            InstanceID = Guid.NewGuid().ToString();
            BaseItem = baseItem;

            if (BaseItem != null && BaseItem.Components != null)
            {
                foreach (var component in BaseItem.Components)
                {
                    var initialState = component.CreateInitialState();
                    if (initialState != null)
                    {
                        States.Add(initialState);
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves a specific state at runtime.
        /// </summary>
        public T GetState<T>() where T : ItemComponentState
        {
            foreach (var state in States)
            {
                if (state is T typedState)
                    return typedState;
            }
            return null;
        }

        /// <summary>
        /// Checks if this ItemInstance is effectively identical to another.
        /// </summary>
        public bool IsStackableWith(ItemInstance other)
        {
            if (other == null) return false;

            // 1. They must be the exact same blueprint
            if (BaseItem != other.BaseItem) return false;

            // 2. If one has an extra state, they do not match
            if (States.Count != other.States.Count) return false;

            // 3. Compare every state dynamically
            foreach (var myState in States)
            {
                var otherState = other.GetState(myState.GetType());

                // If the other item lacks this state, or the states report they don't match
                if (otherState == null || !myState.IsStackableWith(otherState))
                {
                    return false;
                }
            }

            return true; // All checks passed, items can merge
        }

        // Internal helper to find a state by its raw System.Type
        private ItemComponentState GetState(Type type)
        {
            foreach (var state in States)
            {
                if (state.GetType() == type) return state;
            }
            return null;
        }

        public ItemInstance Clone()
        {
            ItemInstance clonedItem = new ItemInstance(this.BaseItem);
            clonedItem.InstanceID = System.Guid.NewGuid().ToString();

            clonedItem.States = new System.Collections.Generic.List<ItemComponentState>();
            foreach (var state in this.States)
            {
                clonedItem.States.Add(state.Clone());
            }

            return clonedItem;
        }
    }
}
