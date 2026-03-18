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
    }
}