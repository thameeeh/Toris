using OutlandHaven.Inventory;
using UnityEngine;

public static class PlayerCombatCalculator
{
    public static PlayerAttackComputedStats CalculateAttack(ItemInstance weaponItem, PlayerStats playerStats)
    {
        return CalculateAttack(weaponItem, playerStats, 0f);
    }

    public static PlayerAttackComputedStats CalculateAttack(
        ItemInstance weaponItem,
        PlayerStats playerStats,
        float bowDamageContribution)
    {
        PlayerAttackComputedStats result = default;

        if (weaponItem == null || playerStats == null)
            return result;

        WeaponComputedStats weaponStats = EquippedItemStatCalculator.CalculateWeapon(weaponItem);
        if (!weaponStats.IsValid)
            return result;

        float validatedBowDamage = Mathf.Max(0f, bowDamageContribution);
        float outgoingMultiplier = playerStats.ResolvedEffects.outgoingDamageMultiplier;

        result.BowDamage = validatedBowDamage;
        result.WeaponDamage = weaponStats.FinalWeaponDamage;
        result.OutgoingDamageMultiplier = outgoingMultiplier;
        result.FinalAttackDamage = (validatedBowDamage + weaponStats.FinalWeaponDamage) * outgoingMultiplier;
        result.IsValid = true;

        return result;
    }
}