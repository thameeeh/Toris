using UnityEngine;
using OutlandHaven.UIToolkit;
using System;

namespace OutlandHaven.Inventory
{
    public enum EquipmentSlot
    {
        Head,
        Chest,
        Legs,
        Arms,
        Weapon
    }

    [Serializable]
    public class EquipableComponent : ItemComponent, IEquipable
    {
        public EquipmentSlot TargetSlot;
        public float StrengthBonus;
        public float DefenceBonus;
        [Tooltip("Flat max health added while this item is equipped.")]
        public float MaxHealthBonus;
        [Tooltip("Flat max stamina added while this item is equipped.")]
        public float MaxStaminaBonus;

        EquipmentSlot IEquipable.TargetSlot => TargetSlot;
        float IEquipable.StrengthBonus => StrengthBonus;
        float IEquipable.DefenceBonus => DefenceBonus;
        float IEquipable.MaxHealthBonus => MaxHealthBonus;
        float IEquipable.MaxStaminaBonus => MaxStaminaBonus;

        public override string GetStackingValidationMessage(InventoryItemSO owner, int maxStackSize)
        {
            if (maxStackSize > 1)
            {
                return $"Equippable item '{owner.ItemName}' has MaxStackSize={maxStackSize}. Equippables must always have MaxStackSize=1.";
            }
            return null;
        }

        public override int GetMaxStackSizeLimit()
        {
            return 1;
        }
    }
}
