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
    public class ConsumableComponent : ItemComponent, IUsable
    {
        [Tooltip("How this consumable applies its gameplay effect.")]
        public ConsumableEffectMode EffectMode = ConsumableEffectMode.InstantResource;

        [Tooltip("The Type of the effect to trigger.")]
        public ConsumptionSlot EffectPayload;

        [Tooltip("Amount of resources to add.")]
        public int amount = 20;

        [Tooltip("Timed player effect definition to apply while the buff is active.")]
        public PlayerEffectDefinitionSO TimedEffectDefinition;

        [Tooltip("Optional Resources path used if the direct timed effect reference is unavailable.")]
        public string TimedEffectDefinitionResourcePath;

        [Tooltip("Duration in seconds for timed player effects.")]
        [Min(0f)] public float TimedEffectDuration = 5f;

        [Tooltip("Cooldown in seconds before this item can be used again.")]
        public float CooldownDuration = 1.5f;

        public PlayerEffectDefinitionSO ResolveTimedEffectDefinition()
        {
            if (TimedEffectDefinition != null)
                return TimedEffectDefinition;

            if (string.IsNullOrWhiteSpace(TimedEffectDefinitionResourcePath))
                return null;

            TimedEffectDefinition = Resources.Load<PlayerEffectDefinitionSO>(TimedEffectDefinitionResourcePath);
            return TimedEffectDefinition;
        }

        public override string GetStackingValidationMessage(InventoryItemSO owner, int maxStackSize)
        {
            return null;
        }

        public bool TryUse(PlayerConsumableController consumableController, InventoryManager inventoryManager, InventorySlot slot)
        {
            // The logic from PlayerConsumableController has been migrated directly into the interface method.
            if (consumableController == null || slot == null || slot.IsEmpty || slot.HeldItem == null)
            {
                return false;
            }

            return consumableController.ExecuteConsumption(this, slot);
        }
    }
}
