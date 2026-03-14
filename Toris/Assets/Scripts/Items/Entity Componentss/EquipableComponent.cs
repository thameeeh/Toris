using UnityEngine;
using OutlandHaven.UIToolkit;
using System;

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
    public int StreangthBonus;
    public int DefenceBonus;
    public float MaxDurability = 100f;

    public override ItemComponentState CreateInitialState()
    {
        return new DurabilityState(MaxDurability);
    }
}
