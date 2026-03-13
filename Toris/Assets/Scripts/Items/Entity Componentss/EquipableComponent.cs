using UnityEngine;

public enum EquipmentSlot 
{
    Head,
    Chest,
    Legs,
    Arms,
    Weapon
}

public class EquipableComponent : ItemComponent
{
    public EquipmentSlot TargetSlot;
    public int StreangthBonus;
    public int DefenceBonus;
}
