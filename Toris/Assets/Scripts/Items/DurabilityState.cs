using System;

namespace OutlandHaven.UIToolkit
{
    [Serializable]
    public class DurabilityState : ItemComponentState
    {
        public float CurrentDurability;

        public DurabilityState(float durability)
        {
            CurrentDurability = durability;
        }

        public override bool IsStackableWith(ItemComponentState other)
        {
            if (other is DurabilityState otherDurability)
            {
                // They only stack if the durability numbers match exactly
                // (e.g., two 100% durability swords can technically stack if MaxStackSize > 1)
                return this.CurrentDurability == otherDurability.CurrentDurability;
            }
            return false;
        }
    }
}
