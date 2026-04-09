using UnityEngine;
using OutlandHaven.Inventory;

public class WeaponComputedStatsDebugger : MonoBehaviour
{
    [SerializeField] private PlayerEquipmentController _equipment;

    [ContextMenu("Log Weapon Computed Stats")]
    public void LogWeaponComputedStats()
    {
        if (_equipment == null)
        {
            Debug.LogWarning("[WeaponComputedStatsDebugger] Missing PlayerEquipmentController reference.");
            return;
        }

        ItemInstance item = _equipment.GetEquippedItem(EquipmentSlot.Weapon);
        if (item == null)
        {
            Debug.Log("[WeaponComputedStatsDebugger] No weapon equipped.");
            return;
        }

        WeaponComputedStats stats = EquippedItemStatCalculator.CalculateWeapon(item);

        Debug.Log(
            $"[WeaponComputedStatsDebugger]\n" +
            $"BaseDamage: {stats.BaseDamage}\n" +
            $"AttackSpeed: {stats.AttackSpeed}\n" +
            $"StrengthBonus: {stats.StrengthBonus}\n" +
            $"UpgradeLevel: {stats.UpgradeLevel}\n" +
            $"IsAwakened: {stats.IsAwakened}\n" +
            $"AwakenedDamageBonus: {stats.AwakenedDamageBonus}\n" +
            $"UpgradeDamageBonus: {stats.UpgradeDamageBonus}\n" +
            $"StrengthDamageBonus: {stats.StrengthDamageBonus}\n" +
            $"FinalWeaponDamage: {stats.FinalWeaponDamage}"
        );
    }
}
