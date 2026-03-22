using OutlandHaven.Inventory;

public static class PlayerCombatCalculator
{
    public static PlayerAttackComputedStats CalculateAttack(ItemInstance weaponItem, PlayerStats playerStats)
    {
        PlayerAttackComputedStats result = default;

        if (weaponItem == null || playerStats == null)
            return result;

        WeaponComputedStats weaponStats = EquippedItemStatCalculator.CalculateWeapon(weaponItem);
        if (!weaponStats.IsValid)
            return result;

        float outgoingMultiplier = playerStats.ResolvedEffects.outgoingDamageMultiplier;

        result.WeaponDamage = weaponStats.FinalWeaponDamage;
        result.OutgoingDamageMultiplier = outgoingMultiplier;
        result.FinalAttackDamage = weaponStats.FinalWeaponDamage * outgoingMultiplier;
        result.IsValid = true;

        return result;
    }
}