using System;
using UnityEngine;

namespace OutlandHaven.Inventory
{
    public enum ConsumptionSlot
    {
        HP,
        Mana
    }

    public enum ConsumableEffectMode
    {
        InstantResource,
        TimedPlayerEffect
    }

    // --- THE BLUEPRINT (Static Rules) ---
    [Serializable]
    public class ConsumableComponent : ItemComponent
    {
        [Tooltip("How this consumable applies its gameplay effect.")]
        public ConsumableEffectMode EffectMode = ConsumableEffectMode.InstantResource;

        [Tooltip("The Type of the effect to trigger.")]
        public ConsumptionSlot EffectPayload;

        [Tooltip("Amount of resources to add.")]
        public int amount = 20;

        [Tooltip("Timed player effect definition to apply while the buff is active.")]
        public PlayerEffectDefinitionSO TimedEffectDefinition;

        [Tooltip("Duration in seconds for timed player effects.")]
        [Min(0f)] public float TimedEffectDuration = 5f;

        [Tooltip("Cooldown in seconds before this item can be used again.")]
        public float CooldownDuration = 1.5f;

        [Tooltip("How many times can this item be used before it is destroyed? If this is greater than 1, the item should not stack in inventory.")]
        public int MaxCharges = 1;

        public override ItemComponentState CreateInitialState()
        {
            return new ConsumableState(MaxCharges);
        }

        public override string GetStackingValidationMessage(InventoryItemSO owner, int maxStackSize)
        {
            if (MaxCharges <= 1 || maxStackSize <= 1)
                return null;

            string itemName = owner != null ? owner.ItemName : "Unknown Item";
            return $"[InventoryItemSO] '{itemName}' uses MaxCharges={MaxCharges} and should not stack in inventory. Set MaxStackSize to 1. Loot tables control drop quantity; MaxStackSize only controls how many copies fit in one inventory slot.";
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
