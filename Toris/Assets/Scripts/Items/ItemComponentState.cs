using System;

namespace OutlandHaven.Inventory
{
    [Serializable]
    public abstract class ItemComponentState
    {
        // Every unique state must declare if it matches another state.
        public abstract bool IsStackableWith(ItemComponentState other);

        public abstract ItemComponentState Clone();
    }
}