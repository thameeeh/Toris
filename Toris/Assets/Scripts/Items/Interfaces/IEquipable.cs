using UnityEngine;

namespace OutlandHaven.Inventory
{
    public interface IEquipable
    {
        EquipmentSlot TargetSlot { get; }
        float StrengthBonus { get; }
        float DefenceBonus { get; }
        float MaxHealthBonus { get; }
        float MaxStaminaBonus { get; }
    }
}
