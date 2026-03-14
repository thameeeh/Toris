using System;

namespace OutlandHaven.UIToolkit
{
    [Serializable]
    public abstract class ItemComponentState
    {
        // Every unique state must declare if it matches another state.
        public abstract bool IsStackableWith(ItemComponentState other);
    }
}