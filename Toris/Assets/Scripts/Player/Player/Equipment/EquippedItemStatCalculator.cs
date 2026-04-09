using OutlandHaven.Inventory;
using UnityEngine;

public static class EquippedItemStatCalculator
{
    public static EquippedItemComputedStats Calculate(ItemInstance item)
    {
        EquippedItemComputedStats result = default;

        if (item == null || item.BaseItem == null)
            return result;

        EquipableComponent equipable = item.BaseItem.GetComponent<EquipableComponent>();
        OffensiveComponent offensive = item.BaseItem.GetComponent<OffensiveComponent>();
        DefensiveComponent defensive = item.BaseItem.GetComponent<DefensiveComponent>();
        EvolvingComponent evolving = item.BaseItem.GetComponent<EvolvingComponent>();

        UpgradeableState upgradeState = item.GetState<UpgradeableState>();
        EvolvingState evolvingState = item.GetState<EvolvingState>();

        if (equipable != null)
        {
            result.StrengthBonus = equipable.StrengthBonus;
            result.DefenceBonus = equipable.DefenceBonus;
        }

        if (offensive != null)
        {
            result.BaseDamage = offensive.BaseDamage;
            result.AttackSpeed = offensive.AttackSpeed;
        }

        if (defensive != null)
        {
            result.PhysicalDefense = defensive.PhysicalDefense;
            result.MagicalDefense = defensive.MagicalDefense;
        }

        result.UpgradeLevel = upgradeState != null ? upgradeState.CurrentLevel : 1;
        result.IsAwakened = evolvingState != null && evolvingState.IsAwakened;
        result.AwakenedDamageBonus =
            (evolving != null && result.IsAwakened)
            ? evolving.AwakenedDamageBonus
            : 0f;

        result.IsValid = true;
        return result;
    }

    public static WeaponComputedStats CalculateWeapon(ItemInstance item)
    {
        WeaponComputedStats result = default;

        EquippedItemComputedStats baseStats = Calculate(item);
        if (!baseStats.IsValid)
            return result;

        result.BaseDamage = baseStats.BaseDamage;
        result.AttackSpeed = baseStats.AttackSpeed;
        result.StrengthBonus = baseStats.StrengthBonus;
        result.UpgradeLevel = baseStats.UpgradeLevel;
        result.IsAwakened = baseStats.IsAwakened;
        result.AwakenedDamageBonus = baseStats.AwakenedDamageBonus;

        result.UpgradeDamageBonus = Mathf.Max(0, result.UpgradeLevel - 1) * 2f;
        result.StrengthDamageBonus = result.StrengthBonus * 0.5f;

        result.FinalWeaponDamage =
            result.BaseDamage +
            result.UpgradeDamageBonus +
            result.AwakenedDamageBonus +
            result.StrengthDamageBonus;

        result.IsValid = true;
        return result;
    }
}
