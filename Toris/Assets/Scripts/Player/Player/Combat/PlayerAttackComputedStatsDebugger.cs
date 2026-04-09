using UnityEngine;
using OutlandHaven.Inventory;

public class PlayerAttackComputedStatsDebugger : MonoBehaviour
{
    [SerializeField] private PlayerEquipmentController _equipment;
    [SerializeField] private PlayerStats _playerStats;

    [ContextMenu("Log Final Attack Damage")]
    public void LogFinalAttackDamage()
    {
        if (_equipment == null || _playerStats == null)
        {
            Debug.LogWarning("[PlayerAttackComputedStatsDebugger] Missing references.");
            return;
        }

        ItemInstance weaponItem = _equipment.GetEquippedItem(EquipmentSlot.Weapon);
        if (weaponItem == null)
        {
            Debug.Log("[PlayerAttackComputedStatsDebugger] No weapon equipped.");
            return;
        }

        PlayerAttackComputedStats attackStats =
            PlayerCombatCalculator.CalculateAttack(weaponItem, _playerStats);

        if (!attackStats.IsValid)
        {
            Debug.Log("[PlayerAttackComputedStatsDebugger] Attack calculation was invalid.");
            return;
        }

        Debug.Log(
            $"[PlayerAttackComputedStatsDebugger]\n" +
            $"BowDamage: {attackStats.BowDamage}\n" +
            $"WeaponDamage: {attackStats.WeaponDamage}\n" +
            $"OutgoingDamageMultiplier: {attackStats.OutgoingDamageMultiplier}\n" +
            $"FinalAttackDamage: {attackStats.FinalAttackDamage}"
        );
    }

    [ContextMenu("Debug Upgrade Equipped Weapon")]
    public void DebugUpgradeEquippedWeapon()
    {
        if (_equipment == null)
            return;

        ItemInstance weaponItem = _equipment.GetEquippedItem(EquipmentSlot.Weapon);
        if (weaponItem == null)
            return;

        UpgradeableState upgradeState = weaponItem.GetState<UpgradeableState>();
        if (upgradeState == null)
            return;

        upgradeState.CurrentLevel++;
        weaponItem.NotifyStateChanged();

        Debug.Log($"[PlayerAttackComputedStatsDebugger] Debug upgraded equipped weapon to level {upgradeState.CurrentLevel}");
    }
}
