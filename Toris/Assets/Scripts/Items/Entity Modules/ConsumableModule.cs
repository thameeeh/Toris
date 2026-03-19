using System;
using UnityEngine;

namespace OutlandHaven.Inventory
{
    public enum ConsumptionSlot
    {
        HP,
        Mana
    }

    // --- THE BLUEPRINT (Static Rules) ---
    [Serializable]
    public class ConsumableComponent : ItemComponent
    {
        [Tooltip("The Type of the effect to trigger.")]
        public ConsumptionSlot EffectPayload;

        [Tooltip("Amount of resources to add.")]
        public int amount = 20;

        [Tooltip("Cooldown in seconds before this item can be used again.")]
        public float CooldownDuration = 1.5f;

        [Tooltip("How many times can this item be used before it is destroyed?")]
        public int MaxCharges = 1;

        public override ItemComponentState CreateInitialState()
        {
            return new ConsumableState(MaxCharges);
        }
    }

    // --- THE RUNTIME TRACKER (Live Data) ---
    [Serializable]
    public class ConsumableState : ItemComponentState
    {
        public int CurrentCharges;

        public ConsumableState(int startingCharges)
        {
            CurrentCharges = startingCharges;
        }

        public override bool IsStackableWith(ItemComponentState other)
        {
            if (other is ConsumableState otherConsumable)
            {
                // Only stack if they have the exact same number of uses left
                return this.CurrentCharges == otherConsumable.CurrentCharges;
            }
            return false;
        }

        public override ItemComponentState Clone()
        {
            return new ConsumableState(this.CurrentCharges);
        }
    }
}