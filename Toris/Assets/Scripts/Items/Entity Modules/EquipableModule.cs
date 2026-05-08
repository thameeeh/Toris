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
    public class EquipableComponent : ItemComponent
    {
        public EquipmentSlot TargetSlot;
        public float StrengthBonus;
        public float DefenceBonus;

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