using UnityEngine;
using OutlandHaven.Inventory;

public class EquippedItemStatsDebugger : MonoBehaviour
{
    [SerializeField] private PlayerEquipmentController _equipment;

    [ContextMenu("Log Equipped Weapon Stats")]
    public void LogEquippedWeaponStats()
    {
        if (_equipment == null)
        {
            Debug.LogWarning("[EquippedItemStatsDebugger] Missing PlayerEquipmentController reference.");
            return;
        }

        ItemInstance item = _equipment.GetEquippedItem(EquipmentSlot.Weapon);
        if (item == null)
        {
            Debug.Log("[EquippedItemStatsDebugger] No weapon equipped.");
            return;
        }

        EquippedItemComputedStats stats = EquippedItemStatCalculator.Calculate(item);

        Debug.Log(
            $"[EquippedItemStatsDebugger] Equipped Weapon Stats\n" +
            $"Item: {item.BaseItem.ItemName}\n" +
            $"StrengthBonus: {stats.StrengthBonus}\n" +
            $"DefenceBonus: {stats.DefenceBonus}\n" +
            $"BaseDamage: {stats.BaseDamage}\n" +
            $"AttackSpeed: {stats.AttackSpeed}\n" +
            $"PhysicalDefense: {stats.PhysicalDefense}\n" +
            $"MagicalDefense: {stats.MagicalDefense}\n" +
            $"UpgradeLevel: {stats.UpgradeLevel}\n" +
            $"IsAwakened: {stats.IsAwakened}\n" +
            $"AwakenedDamageBonus: {stats.AwakenedDamageBonus}"
        );
    }
}